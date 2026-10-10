using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Queries;

public enum GetReorderSuggestionsError { NotSignedIn }

/// <summary>
/// Products at or below their reorder level, with a suggested quantity that brings stock up to twice the reorder
/// level, which is what the purchase order dialog starts from.
/// </summary>
public class GetReorderSuggestionsQuery : IRequest<Result<IReadOnlyList<ReorderLine>>>
{
}

public class GetReorderSuggestionsQueryHandler : IRequestHandler<GetReorderSuggestionsQuery, Result<IReadOnlyList<ReorderLine>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetReorderSuggestionsQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<IReadOnlyList<ReorderLine>>> Handle(GetReorderSuggestionsQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetReorderSuggestionsError.NotSignedIn, Access.NotSignedIn);

        var products = await _dbContext.Products.AsNoTracking().Include(p => p.Batches)
            .Where(p => p.IsStocked && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyList<ReorderLine> lines = products
            .Where(p => p.ReorderLevel > 0 && p.OnHand <= p.ReorderLevel)
            .Select(p => new ReorderLine(p.Id, p.Name, p.Unit, p.IsControlledDrug, p.OnHand, p.ReorderLevel,
                Math.Max(1, Math.Ceiling(p.ReorderLevel * 2 - p.OnHand))))
            .ToList();
        return Result.Success(lines);
    }
}
