using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Domain.Services;
using SimpleResult;

namespace VetDesk.Application.Invoices.Queries;

public enum GetInvoicePrintoutError { NotAllowed, NotFound }

public class GetInvoicePrintoutQuery : IRequest<Result<InvoicePrintout>>
{
    public int Id { get; init; }
}

public class GetInvoicePrintoutQueryHandler : IRequestHandler<GetInvoicePrintoutQuery, Result<InvoicePrintout>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public GetInvoicePrintoutQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<InvoicePrintout>> Handle(GetInvoicePrintoutQuery request, CancellationToken cancellationToken)
    {
        // Whoever can open an invoice can print it.
        if (_user.Lacks(Permission.TakePayments) && _user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(GetInvoicePrintoutError.NotAllowed, Access.Denied(Permission.TakePayments));

        var invoice = await InvoiceQueries.LoadAsync(_dbContext, request.Id, tracking: false, cancellationToken);
        if (invoice is null)
            return Result.Failure(GetInvoicePrintoutError.NotFound, "That invoice no longer exists.");

        var s = await _dbContext.ClinicSettings.AsNoTracking().FirstAsync(cancellationToken);
        var practice = new PracticeDetails(s.Name, s.Address, s.Phone, s.Email, s.VatNumber, s.CompanyName, s.CompanyNumber,
            s.CompanyRegisteredIn, s.RegisteredOffice, s.BankAccountName, s.BankSortCode, s.BankAccountNumber, s.PaymentTermsDays);

        PrintoutPatient? patient = null;
        if (invoice.Patient is { } p)
        {
            var visit = invoice.ConsultationId is { } consultationId
                ? await _dbContext.Consultations.AsNoTracking()
                    .Where(c => c.Id == consultationId)
                    .Select(c => new { c.Clinician.FirstName, c.Clinician.LastName, c.StartedUtc })
                    .FirstOrDefaultAsync(cancellationToken)
                : null;
            var seenBy = visit is null ? null : string.IsNullOrWhiteSpace(visit.LastName) ? visit.FirstName : $"{visit.FirstName} {visit.LastName}";
            patient = new PrintoutPatient(p.Name, p.PatientNumber, p.SpeciesAndBreed, seenBy, visit is null ? null : Utc.Mark(visit.StartedUtc));
        }

        // Lines are never edited after the invoice is raised, so this is the same calculation that set the stored totals.
        var vatByRate = VatCalculator.ByRate(invoice.Lines);

        return Result.Success(new InvoicePrintout(InvoiceQueries.ToDetail(invoice, ClinicTime.Today(_clock.UtcNow)), practice, patient, vatByRate));
    }
}
