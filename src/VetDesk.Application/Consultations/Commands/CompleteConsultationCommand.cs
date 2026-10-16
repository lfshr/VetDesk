using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Invoices;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Dispensing;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Domain.Entities.Tasks;
using VetDesk.Domain.Services;
using SimpleResult;

namespace VetDesk.Application.Consultations.Commands;

public enum CompleteConsultationError { NotAllowed, NotFound, AlreadyCompleted, IncompleteNotes, AllergyConflict, ChangedElsewhere }

public record CompletedConsultation(int? InvoiceId, string? InvoiceNumber, decimal Total, int VaccinationsRecorded, int ItemsToDispense);

/// <summary>
/// "Complete and invoice". In one save: locks the consultation, raises the invoice from its charges, puts vaccines
/// on the vaccination record, records the weight, sends stocked items to the pharmacy queue (where stock is taken
/// off and controlled drugs are witnessed), and marks the appointment done.
/// </summary>
public class CompleteConsultationCommand : IRequest<Result<CompletedConsultation>>
{
    public int Id { get; init; }
    public Guid Version { get; init; }

    /// <summary>Adds a follow-up to book a recheck this many days from today.</summary>
    public int? RecheckInDays { get; init; }
}

public class CompleteConsultationCommandHandler : IRequestHandler<CompleteConsultationCommand, Result<CompletedConsultation>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public CompleteConsultationCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<CompletedConsultation>> Handle(CompleteConsultationCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(CompleteConsultationError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));

        var consultation = await _dbContext.Consultations
            .Include(c => c.Patient).ThenInclude(p => p.Client)
            .Include(c => c.Patient).ThenInclude(p => p.Allergies)
            .Include(c => c.Appointment)
            .Include(c => c.Clinician)
            .Include(c => c.Charges).ThenInclude(ch => ch.Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (consultation is null)
            return Result.Failure(CompleteConsultationError.NotFound, "That consultation no longer exists.");
        if (consultation.IsLocked)
            return Result.Failure(CompleteConsultationError.AlreadyCompleted, "This consultation is already completed.");
        if (consultation.Version != request.Version)
            return Result.Failure(CompleteConsultationError.ChangedElsewhere, Saving.ChangedElsewhere);
        if (!consultation.HasNotes)
            return Result.Failure(CompleteConsultationError.IncompleteNotes, "Write the clinical notes before completing.");

        // Checked again here: an allergy may have been recorded after the item was added.
        var patient = consultation.Patient;
        var unresolved = consultation.Charges
            .Where(ch => ch.AllergyOverrideReason is null && AllergyCheck.Conflicts(ch.Product, patient.Allergies).Count > 0)
            .ToList();
        if (unresolved.Count > 0)
            return Result.Failure(CompleteConsultationError.AllergyConflict,
                $"{AllergyCheck.Describe(unresolved[0].Product, AllergyCheck.Conflicts(unresolved[0].Product, patient.Allergies))}. Remove it or have a vet override it.");

        var now = _clock.UtcNow;
        var today = ClinicTime.Today(now);

        Invoice? invoice = null;
        if (consultation.Charges.Count > 0)
        {
            invoice = await InvoiceFactory.CreateAsync(_dbContext, patient.Client, patient,
                consultation.Charges.OrderBy(ch => ch.Id).Select(ch => new InvoiceLine
                {
                    Product = ch.Product,
                    Description = ch.Description,
                    Quantity = ch.Quantity,
                    UnitPrice = ch.UnitPrice,
                    VatRate = ch.VatRate,
                }), now, cancellationToken);
        }

        var vaccinations = 0;
        foreach (var charge in consultation.Charges.Where(ch => ch.Product.Category == ProductCategory.Vaccine))
        {
            _dbContext.Vaccinations.Add(new Vaccination
            {
                Patient = patient,
                VaccineName = charge.Product.Name,
                GivenUtc = now,
                GivenBy = consultation.Clinician.ShortName,
                DueDate = charge.Product.BoosterMonths is { } months ? today.AddMonths(months) : null,
                ConsultationId = consultation.Id,
            });
            vaccinations++;
        }

        if (consultation.WeightKg is { } kg)
            _dbContext.WeightRecords.Add(new WeightRecord
            {
                Patient = patient, Kg = kg, RecordedUtc = now, Context = consultation.Reason ?? "Consultation", RecordedBy = _user.Name,
            });

        var toDispense = consultation.Charges.Where(ch => ch.Product.IsStocked).ToList();
        foreach (var charge in toDispense)
            _dbContext.DispenseItems.Add(new DispenseItem
            {
                Patient = patient,
                Product = charge.Product,
                Consultation = consultation,
                Quantity = charge.Quantity,
                Directions = charge.Directions,
                PrescribedBy = consultation.Clinician.ShortName,
            });

        if (request.RecheckInDays is > 0 and var days)
            _dbContext.FollowUps.Add(new FollowUp
            {
                Text = $"Book recheck for {patient.Name} ({consultation.Reason ?? "consultation"})",
                Patient = patient,
                DueDate = today.AddDays(days),
            });

        if (consultation.Appointment is { } appointment)
        {
            if (appointment.Status is AppointmentStatus.Booked or AppointmentStatus.CheckedIn)
                appointment.ChangeStatus(AppointmentStatus.InConsult, now);
            appointment.ChangeStatus(AppointmentStatus.Done, now);
        }

        Notify.Add(_dbContext, now,
            invoice is null ? $"{patient.Name}'s consultation completed" : $"{patient.Name}'s consultation completed, {invoice.Number} raised",
            NotificationKind.Success, $"/patients/{patient.Id}");

        if (invoice is not null)
            invoice.ConsultationId = consultation.Id;
        consultation.MarkCompleted(now, invoice);
        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(CompleteConsultationError.ChangedElsewhere, Saving.ChangedElsewhere);

        return Result.Success(new CompletedConsultation(invoice?.Id, invoice?.Number, invoice?.Total ?? 0, vaccinations, toDispense.Count));
    }
}
