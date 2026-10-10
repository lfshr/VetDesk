using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Commands;

public enum RecordBalanceCheckError { NotAllowed, InvalidRequest, NotFound, WitnessRequired, Discrepancy, ChangedElsewhere }

/// <summary>
/// A witnessed count of a controlled drug. When the count doesn't match the register a reason is required, and the
/// stock is corrected with an adjustment entry so the discrepancy stays visible in the register.
/// </summary>
public class RecordBalanceCheckCommand : IRequest<Result<decimal>>
{
    public int ProductId { get; init; }
    public decimal Counted { get; init; }
    public int? WitnessId { get; init; }
    public string? WitnessPin { get; init; }
    public string? DiscrepancyReason { get; init; }
}

public class RecordBalanceCheckCommandHandler : IRequestHandler<RecordBalanceCheckCommand, Result<decimal>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IPasswordHasher _hasher;

    public RecordBalanceCheckCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock, IPasswordHasher hasher)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
        _hasher = hasher;
    }

    /// <returns>The difference between counted and expected (zero when it balances).</returns>
    public async Task<Result<decimal>> Handle(RecordBalanceCheckCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WitnessControlledDrugs))
            return Result.Failure(RecordBalanceCheckError.NotAllowed, Access.Denied(Permission.WitnessControlledDrugs));
        if (request.Counted < 0)
            return Result.Failure(RecordBalanceCheckError.InvalidRequest, "The count can't be negative.");

        var product = await _dbContext.Products.Include(p => p.Batches)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsControlledDrug, cancellationToken);
        if (product is null)
            return Result.Failure(RecordBalanceCheckError.NotFound, "Choose a controlled drug.");

        var (witness, problem) = await WitnessCheck.VerifyAsync(_dbContext, _hasher, _user, request.WitnessId, request.WitnessPin, _clock.UtcNow, cancellationToken);
        if (problem is not null)
            return Result.Failure(RecordBalanceCheckError.WitnessRequired, problem);

        var now = _clock.UtcNow;
        var difference = request.Counted - product.OnHand;
        if (difference != 0)
        {
            if (Text.Clean(request.DiscrepancyReason) is not { } reason)
                return Result.Failure(RecordBalanceCheckError.Discrepancy,
                    $"The register says {product.OnHand:0.##} {product.Unit} but {request.Counted:0.##} were counted. Give a reason to record the difference.");

            StockLedger.Adjust(_dbContext, product, request.Counted, "CD balance check", _user.Name, now);
            StockLedger.Register(_dbContext, product, ControlledDrugEntryKind.Adjustment, difference, $"Discrepancy: {reason}", _user.Name, witness!.ShortName, now);
        }

        StockLedger.Register(_dbContext, product, ControlledDrugEntryKind.BalanceCheck, 0, "Balance check", _user.Name, witness!.ShortName, now);

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(RecordBalanceCheckError.ChangedElsewhere, "Stock changed at another desk during the count. Count again.");
        return Result.Success(difference);
    }
}
