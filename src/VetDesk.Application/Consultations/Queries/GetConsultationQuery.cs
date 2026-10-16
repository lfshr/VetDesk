using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Domain.Services;
using SimpleResult;

namespace VetDesk.Application.Consultations.Queries;

public enum GetConsultationError { NotAllowed, NotFound }

public class GetConsultationQuery : IRequest<Result<ConsultationDetail>>
{
    public int Id { get; init; }
}

public class GetConsultationQueryHandler : IRequestHandler<GetConsultationQuery, Result<ConsultationDetail>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public GetConsultationQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<ConsultationDetail>> Handle(GetConsultationQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(GetConsultationError.NotAllowed, Access.Denied(Permission.ViewClinicalRecords));

        var c = await _dbContext.Consultations.AsNoTracking()
            .Include(x => x.Patient).ThenInclude(p => p.Client)
            .Include(x => x.Patient).ThenInclude(p => p.Allergies)
            .Include(x => x.Patient).ThenInclude(p => p.Alerts)
            .Include(x => x.Clinician)
            .Include(x => x.Appointment)
            .Include(x => x.Charges).ThenInclude(ch => ch.Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (c is null)
            return Result.Failure(GetConsultationError.NotFound, "That consultation no longer exists.");

        var invoiceNumber = c.InvoiceId is { } invoiceId
            ? await _dbContext.Invoices.Where(i => i.Id == invoiceId).Select(i => i.Number).FirstOrDefaultAsync(cancellationToken)
            : null;

        var charges = c.Charges.OrderBy(ch => ch.Id).Select(ch => new ChargeDto(
            ch.Id, ch.Product.Id, ch.Description, ch.Product.Category, ch.Quantity, ch.Product.Unit, ch.UnitPrice, ch.VatRate, ch.Net,
            ch.IsPrescription, ch.Directions, ch.Product.IsControlledDrug, ch.AllergyOverrideReason,
            AllergyCheck.Conflicts(ch.Product, c.Patient.Allergies).Select(a => a.Substance).ToList())).ToList();
        var totals = VatCalculator.Calculate(c.Charges.Select(ch => (ch.Net, ch.VatRate)));

        return Result.Success(new ConsultationDetail(
            c.Id, c.Patient.Id, c.Patient.Name, c.Patient.PatientNumber, c.Patient.SpeciesAndBreed,
            c.Patient.AgeLabel(ClinicTime.Today(_clock.UtcNow), withMonths: true), c.Patient.Client.FullName, c.Patient.Client.Id,
            c.Patient.Allergies.Select(a => a.Substance).ToList(),
            c.Patient.Alerts.OrderBy(a => a.Severity).Select(a => (a.Text, a.Severity)).ToList(),
            c.Appointment?.Id, c.Reason, c.Clinician.ShortName, Utc.Mark(c.StartedUtc), Utc.Mark(c.CompletedUtc), c.Status,
            c.Notes, c.OwnerAdvice, c.TemperatureC, c.HeartRate, c.RespiratoryRate, c.WeightKg,
            c.MucousMembranes, c.CapillaryRefill, charges, totals.Net, totals.Vat, totals.Total, c.InvoiceId, invoiceNumber, c.Version));
    }
}
