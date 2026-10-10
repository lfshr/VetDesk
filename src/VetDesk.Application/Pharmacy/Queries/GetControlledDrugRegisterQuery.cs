using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Queries;

public enum GetControlledDrugRegisterError { NotSignedIn }

public class GetControlledDrugRegisterQuery : IRequest<Result<IReadOnlyList<ControlledDrugEntryDto>>>
{
    public int? ProductId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Take { get; init; } = 200;
}

public class GetControlledDrugRegisterQueryHandler : IRequestHandler<GetControlledDrugRegisterQuery, Result<IReadOnlyList<ControlledDrugEntryDto>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetControlledDrugRegisterQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<IReadOnlyList<ControlledDrugEntryDto>>> Handle(GetControlledDrugRegisterQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetControlledDrugRegisterError.NotSignedIn, Access.NotSignedIn);

        var query = _dbContext.ControlledDrugEntries.AsNoTracking().Include(e => e.Product).AsQueryable();
        if (request.ProductId is { } productId)
            query = query.Where(e => e.Product.Id == productId);
        if (request.FromUtc is { } from)
            query = query.Where(e => e.Utc >= from);
        if (request.ToUtc is { } to)
            query = query.Where(e => e.Utc < to);

        var entries = await query.OrderByDescending(e => e.Utc).ThenByDescending(e => e.Id).Take(request.Take).ToListAsync(cancellationToken);
        IReadOnlyList<ControlledDrugEntryDto> rows = entries.Select(e => new ControlledDrugEntryDto(
            e.Id, Utc.Mark(e.Utc), e.Product.Id, e.Product.Name, e.Product.Unit, e.Kind, e.Change, e.Balance, e.Reason, e.By, e.Witness)).ToList();
        return Result.Success(rows);
    }
}
