using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Invoices.Queries;

public enum GetInvoiceError { NotAllowed, NotFound }

public class GetInvoiceQuery : IRequest<Result<InvoiceDetail>>
{
    public int Id { get; init; }
}

public class GetInvoiceQueryHandler : IRequestHandler<GetInvoiceQuery, Result<InvoiceDetail>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public GetInvoiceQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<InvoiceDetail>> Handle(GetInvoiceQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.TakePayments) && _user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(GetInvoiceError.NotAllowed, Access.Denied(Permission.TakePayments));

        var invoice = await InvoiceQueries.LoadAsync(_dbContext, request.Id, tracking: false, cancellationToken);
        if (invoice is null)
            return Result.Failure(GetInvoiceError.NotFound, "That invoice no longer exists.");

        return Result.Success(InvoiceQueries.ToDetail(invoice, ClinicTime.Today(_clock.UtcNow)));
    }
}

internal static class InvoiceQueries
{
    public static Task<Invoice?> LoadAsync(IVetDeskDbContext db, int id, bool tracking, CancellationToken cancellationToken)
    {
        var query = db.Invoices
            .Include(i => i.Client)
            .Include(i => i.Patient)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.Claim)
            .AsSplitQuery();
        return (tracking ? query : query.AsNoTracking()).FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public static InvoiceDetail ToDetail(Invoice i, DateOnly today) => new(
        i.Id, i.Number, Utc.Mark(i.IssuedUtc), i.DueDate, i.Client.Id, i.Client.FullName, i.Client.ClientNumber, i.Client.Address,
        i.Client.Postcode, i.Client.Email, i.Patient?.Id, i.Patient?.Name,
        i.Lines.OrderBy(l => l.Id).Select(l => new InvoiceLineDto(l.Description, l.Quantity, l.UnitPrice, l.VatRate, l.Net)).ToList(),
        i.Net, i.Vat, i.Total, i.Paid, i.AmountDue, i.StatusOn(today),
        i.Payments.OrderBy(p => p.TakenUtc).Select(p => new PaymentDto(p.Id, Utc.Mark(p.TakenUtc), p.Method, p.Amount, p.Reference, p.TakenBy, p.IsRefund)).ToList(),
        i.Claim is { } c ? new ClaimDto(c.Insurer, c.PolicyNumber, c.Excess, c.Condition, Utc.Mark(c.CreatedUtc), c.Status) : null,
        i.Version);
}
