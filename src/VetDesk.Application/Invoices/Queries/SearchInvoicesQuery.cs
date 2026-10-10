using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Invoices.Queries;

public enum SearchInvoicesError { NotAllowed }

public enum InvoiceFilter { All, Unpaid, Insurance, Paid }

public class SearchInvoicesQuery : IRequest<Result<IReadOnlyList<InvoiceRow>>>
{
    public string? Text { get; init; }
    public InvoiceFilter Filter { get; init; }
    public int? ClientId { get; init; }
    public int Take { get; init; } = 200;
}

public class SearchInvoicesQueryHandler : IRequestHandler<SearchInvoicesQuery, Result<IReadOnlyList<InvoiceRow>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public SearchInvoicesQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<InvoiceRow>>> Handle(SearchInvoicesQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.TakePayments))
            return Result.Failure(SearchInvoicesError.NotAllowed, Access.Denied(Permission.TakePayments));

        var query = _dbContext.Invoices.AsNoTracking()
            .Include(i => i.Client)
            .Include(i => i.Patient)
            .Include(i => i.Payments)
            .Include(i => i.Claim)
            .AsQueryable();

        if (request.ClientId is { } clientId)
            query = query.Where(i => i.Client.Id == clientId);
        if (request.Filter == InvoiceFilter.Insurance)
            query = query.Where(i => i.Claim != null);
        if (Text.Clean(request.Text) is { } text)
        {
            var like = Search.Anywhere(text);
            query = query.Where(i => EF.Functions.Like(i.Number, like, Search.EscapeText) ||
                                     EF.Functions.Like(i.Client.FirstName + " " + i.Client.LastName, like, Search.EscapeText) ||
                                     (i.Patient != null && EF.Functions.Like(i.Patient.Name, like, Search.EscapeText)));
        }

        var invoices = await query.OrderByDescending(i => i.IssuedUtc).ThenByDescending(i => i.Id).Take(1000)
            .AsSplitQuery().ToListAsync(cancellationToken);
        var today = ClinicTime.Today(_clock.UtcNow);

        IReadOnlyList<InvoiceRow> rows = invoices
            .Select(i => new InvoiceRow(i.Id, i.Number, Utc.Mark(i.IssuedUtc), i.Client.Id, i.Client.FullName, i.Patient?.Name,
                i.Total, i.AmountDue, i.StatusOn(today)))
            .Where(r => request.Filter switch
            {
                InvoiceFilter.Unpaid => r.AmountDue > 0 && r.Status != InvoiceStatus.Refunded,
                InvoiceFilter.Paid => r.Status is InvoiceStatus.Paid or InvoiceStatus.Refunded,
                _ => true
            })
            .Take(request.Take)
            .ToList();
        return Result.Success(rows);
    }
}
