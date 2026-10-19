using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Consultations;
using VetDesk.Domain.Entities.Dispensing;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Patients.Queries;

public enum GetPatientError { NotAllowed, NotFound }

/// <summary>Everything the patient record screen shows, in one round trip.</summary>
public class GetPatientQuery : IRequest<Result<PatientDetail>>
{
    public int Id { get; init; }
}

public class GetPatientQueryHandler : IRequestHandler<GetPatientQuery, Result<PatientDetail>>
{
    internal const int DueSoonDays = 30;
    internal const int CurrentMedicationDays = 90;

    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public GetPatientQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<PatientDetail>> Handle(GetPatientQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(GetPatientError.NotAllowed, Access.Denied(Permission.ViewClinicalRecords));

        // Identity resolution because Patient → Client → Patients leads back to this patient.
        var patient = await _dbContext.Patients.AsNoTrackingWithIdentityResolution()
            .Include(p => p.Client).ThenInclude(c => c.Patients)
            .Include(p => p.Alerts)
            .Include(p => p.Allergies)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (patient is null)
            return Result.Failure(GetPatientError.NotFound, "That patient no longer exists.");

        var id = patient.Id;
        var today = ClinicTime.Today(_clock.UtcNow);

        var consultations = await _dbContext.Consultations.AsNoTracking()
            .Include(c => c.Clinician)
            .Include(c => c.Appointment).ThenInclude(a => a!.Type)
            .Include(c => c.Charges).ThenInclude(ch => ch.Product)
            .Where(c => c.Patient.Id == id)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        var notes = await _dbContext.ClinicalNotes.AsNoTracking().Where(n => n.Patient.Id == id).ToListAsync(cancellationToken);
        var vaccinations = await _dbContext.Vaccinations.AsNoTracking().Where(v => v.Patient.Id == id).ToListAsync(cancellationToken);
        var weights = await _dbContext.WeightRecords.AsNoTracking().Where(w => w.Patient.Id == id).ToListAsync(cancellationToken);
        var dispensing = await _dbContext.DispenseItems.AsNoTracking().Include(d => d.Product)
            .Where(d => d.Patient.Id == id).ToListAsync(cancellationToken);
        var invoices = await _dbContext.Invoices.AsNoTracking().Include(i => i.Payments).Include(i => i.Claim)
            .Where(i => i.Patient != null && i.Patient.Id == id).ToListAsync(cancellationToken);
        var balance = await Balances.ForClientAsync(_dbContext, patient.Client.Id, cancellationToken);
        // The owner card offers to take payment against the oldest unpaid invoice, for any of their pets.
        var owed = (await _dbContext.Invoices.AsNoTracking().Include(i => i.Payments)
                .Where(i => i.Client.Id == patient.Client.Id).ToListAsync(cancellationToken))
            .Where(i => i.AmountDue > 0).OrderBy(i => i.IssuedUtc).FirstOrDefault();

        var history = consultations.Select(c => new HistoryEntry(
                c.Appointment?.Type.Kind == AppointmentKind.Surgery ? HistoryKind.Surgery : HistoryKind.Consultation,
                Utc.Mark(c.StartedUtc),
                c.Reason ?? c.Appointment?.Reason ?? "Consultation",
                c.Clinician.ShortName,
                Summary(c),
                c.WeightKg,
                c.Id,
                !c.IsLocked,
                c.Charges.Where(ch => ch.Product?.Category is ProductCategory.Medicine or ProductCategory.Vaccine)
                    .Select(ch => new AdministeredDrug(ch.Description, $"{ch.Quantity:0.##} {ch.Product!.Unit}", ch.Product.IsControlledDrug))
                    .ToList()))
            .Concat(notes.Select(n => new HistoryEntry(HistoryKind.Note, Utc.Mark(n.WrittenUtc), n.Title, n.Author, n.Body, null, null, false, [])))
            .Concat(vaccinations.Where(v => v.ConsultationId is null).Select(v => new HistoryEntry(
                HistoryKind.Vaccination, Utc.Mark(v.GivenUtc), v.VaccineName, v.GivenBy,
                v.DueDate is { } due ? $"Next due {due:d MMM yyyy}" : null, null, null, false, [])))
            .OrderByDescending(h => h.Utc)
            .ToList();

        var weightList = weights.Select(w => new WeightDto(Utc.Mark(w.RecordedUtc), w.Kg, w.Context))
            .OrderByDescending(w => w.RecordedUtc).ToList();

        // The latest dose of each vaccine is the one that sets when it's next due.
        var vaccinationList = vaccinations
            .OrderByDescending(v => v.GivenUtc)
            .Select(v => new VaccinationDto(v.Id, v.VaccineName, Utc.Mark(v.GivenUtc), v.GivenBy, v.Batch, v.DueDate,
                v.IsOverdue(today), v.DueDate is { } d && d >= today && d <= today.AddDays(DueSoonDays)))
            .ToList();

        var medicationSince = _clock.UtcNow.AddDays(-CurrentMedicationDays);
        var medication = dispensing
            .Where(d => d.Status == DispenseStatus.Dispensed && d.DispensedUtc >= medicationSince &&
                        d.Product.Category == ProductCategory.Medicine)
            .OrderByDescending(d => d.DispensedUtc)
            .Select(d => new MedicationDto(d.Product.Name, d.Quantity, d.Product.Unit, d.Directions, Utc.Mark(d.DispensedUtc!.Value), d.PrescribedBy))
            .ToList();

        var prescriptions = dispensing
            .OrderByDescending(d => d.CreatedUtc)
            .Select(d => new PrescriptionDto(d.Id, d.Product.Name, d.Quantity, d.Product.Unit, d.Directions, d.PrescribedBy,
                Utc.Mark(d.CreatedUtc), d.Status == DispenseStatus.Pending && d.AwaitingAuthorisation ? "Needs authorisation" : d.Status.ToString()))
            .ToList();

        var invoiceList = invoices
            .OrderByDescending(i => i.IssuedUtc)
            .Select(i => new PatientInvoiceDto(i.Id, i.Number, Utc.Mark(i.IssuedUtc), i.Total, i.AmountDue, i.StatusOn(today).ToString()))
            .ToList();

        var owner = new PatientOwner(patient.Client.Id, patient.Client.ClientNumber, patient.Client.FullName, patient.Client.Phone,
            patient.Client.Email, patient.Client.Address, patient.Client.Postcode, patient.Client.RemindersOptIn, balance,
            patient.Client.Patients.OrderBy(p => p.Name).Select(p => (p.Id, p.Name, p.SpeciesAndBreed)).ToList(),
            owed?.Id, owed?.Number);

        return Result.Success(new PatientDetail(
            patient.Id, patient.Name, patient.PatientNumber, patient.Species, patient.Breed, patient.SpeciesAndBreed,
            patient.Sex, patient.IsNeutered, patient.SexLabel, patient.DateOfBirth, patient.AgeLabel(today, withMonths: true),
            patient.Microchip, patient.IsDeceased, weightList.FirstOrDefault()?.Kg, owner,
            patient.Alerts.OrderBy(a => a.Severity).ThenBy(a => a.Id).Select(a => new AlertDto(a.Id, a.Text, a.Severity, a.PresetKey)).ToList(),
            patient.Allergies.Select(a => new AllergyDto(a.Id, a.Substance, a.DrugClass, a.Label)).ToList(),
            history, weightList, vaccinationList, medication, prescriptions, invoiceList,
            consultations.Where(c => !c.IsLocked).OrderByDescending(c => c.StartedUtc).Select(c => (int?)c.Id).FirstOrDefault(),
            patient.Version));
    }

    private static string? Summary(Consultation c)
    {
        var parts = new[] { c.Assessment, c.Plan }.Where(Text.Has).ToList();
        return parts.Count == 0 ? c.Subjective : string.Join(" ", parts);
    }
}
