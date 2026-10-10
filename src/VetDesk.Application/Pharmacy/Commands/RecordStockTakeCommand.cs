using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Commands;

public enum RecordStockTakeError { NotAllowed, InvalidRequest, ProductNotFound, ControlledDrug, ChangedElsewhere }

public record StockCount(int ProductId, decimal Counted);

/// <summary>
/// Sets stock to what was counted on the shelf. Controlled drugs are counted with a witnessed balance check instead,
/// so the register stays the single record for them.
/// </summary>
public class RecordStockTakeCommand : IRequest<Result<int>>
{
    public IReadOnlyList<StockCount> Counts { get; init; } = [];
}

public class RecordStockTakeCommandHandler : IRequestHandler<RecordStockTakeCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public RecordStockTakeCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    /// <returns>How many products were adjusted.</returns>
    public async Task<Result<int>> Handle(RecordStockTakeCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(RecordStockTakeError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));
        if (request.Counts.Count == 0 || request.Counts.Any(c => c.Counted < 0))
            return Result.Failure(RecordStockTakeError.InvalidRequest, "Enter a count of zero or more for each product.");

        var ids = request.Counts.Select(c => c.ProductId).Distinct().ToList();
        var products = await _dbContext.Products.Include(p => p.Batches).Where(p => ids.Contains(p.Id) && p.IsStocked)
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        if (products.Count != ids.Count)
            return Result.Failure(RecordStockTakeError.ProductNotFound, "One of the products is no longer stocked.");
        if (products.Values.FirstOrDefault(p => p.IsControlledDrug) is { } cd)
            return Result.Failure(RecordStockTakeError.ControlledDrug, $"{cd.Name} is a controlled drug. Record a witnessed balance check instead.");

        var now = _clock.UtcNow;
        var adjusted = 0;
        foreach (var count in request.Counts)
        {
            if (StockLedger.Adjust(_dbContext, products[count.ProductId], count.Counted, $"Stock take {now:yyyy-MM-dd}", _user.Name, now) != 0)
                adjusted++;
        }

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(RecordStockTakeError.ChangedElsewhere, "Stock changed at another desk during the count. Count again.");
        return Result.Success(adjusted);
    }
}
