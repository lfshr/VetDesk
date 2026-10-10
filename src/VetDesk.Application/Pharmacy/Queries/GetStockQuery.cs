using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Products;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Queries;

public enum GetStockError { NotSignedIn }

public enum StockFilter { All, LowStock, ExpiringSoon, ControlledDrugs, Vaccines }

public class GetStockQuery : IRequest<Result<IReadOnlyList<StockRow>>>
{
    internal const int ExpiringSoonDays = 60;

    public string? Text { get; init; }
    public StockFilter Filter { get; init; }
}

public class GetStockQueryHandler : IRequestHandler<GetStockQuery, Result<IReadOnlyList<StockRow>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public GetStockQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<StockRow>>> Handle(GetStockQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetStockError.NotSignedIn, Access.NotSignedIn);

        var query = _dbContext.Products.AsNoTracking().Include(p => p.Batches).Where(p => p.IsStocked && p.IsActive);
        if (Text.Clean(request.Text) is { } text)
        {
            var like = Search.Anywhere(text);
            query = query.Where(p => EF.Functions.Like(p.Name, like, Search.EscapeText) || EF.Functions.Like(p.Code, like, Search.EscapeText) ||
                                     p.Batches.Any(b => EF.Functions.Like(b.BatchNumber, like, Search.EscapeText)));
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
        var today = ClinicTime.Today(_clock.UtcNow);

        IReadOnlyList<StockRow> rows = products.Select(p => ToRow(p, today))
            .Where(r => request.Filter switch
            {
                StockFilter.LowStock => r.Status is StockStatus.Low or StockStatus.Out,
                StockFilter.ExpiringSoon => r.Status is StockStatus.Expiring or StockStatus.Expired,
                StockFilter.ControlledDrugs => r.IsControlledDrug,
                StockFilter.Vaccines => r.Category == ProductCategory.Vaccine,
                _ => true
            })
            .ToList();
        return Result.Success(rows);
    }

    internal static StockRow ToRow(Product p, DateOnly today)
    {
        var next = p.Batches.Where(b => b.Quantity > 0).OrderBy(b => b.Expiry ?? DateOnly.MaxValue).FirstOrDefault();
        var onHand = p.OnHand;
        var status = onHand <= 0 ? StockStatus.Out
            : next?.Expiry is { } expired && expired < today ? StockStatus.Expired
            : next?.Expiry is { } soon && soon <= today.AddDays(GetStockQuery.ExpiringSoonDays) ? StockStatus.Expiring
            : onHand <= p.ReorderLevel ? StockStatus.Low
            : StockStatus.InStock;
        return new StockRow(p.Id, p.Code, p.Name, p.Form, p.Unit, p.StockCategory, p.Category, p.IsControlledDrug, onHand, p.ReorderLevel,
            next?.BatchNumber, next?.Expiry, p.Location, status, p.Batches.Count(b => b.Quantity > 0));
    }
}
