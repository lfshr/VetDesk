using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Dispensing;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Domain.Entities.Tasks;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Commands;

public enum DispenseItemError { NotAllowed, NotFound, AlreadyHandled, NeedsAuthorisation, WitnessRequired, InsufficientStock, ChangedElsewhere }

/// <summary>
/// Hands over (or administers) an item from the queue: takes it out of stock, earliest expiry first. A controlled
/// drug needs a witness who confirms with their PIN, and goes into the register with the new balance.
/// </summary>
public class DispenseItemCommand : IRequest<Result>
{
    public int Id { get; init; }
    public int? WitnessId { get; init; }
    public string? WitnessPin { get; init; }
}

public class DispenseItemCommandHandler : IRequestHandler<DispenseItemCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IPasswordHasher _hasher;

    public DispenseItemCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock, IPasswordHasher hasher)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
        _hasher = hasher;
    }

    public async Task<Result> Handle(DispenseItemCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(DispenseItemError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));

        var item = await _dbContext.DispenseItems
            .Include(d => d.Patient)
            .Include(d => d.Product).ThenInclude(p => p.Batches)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
        if (item is null)
            return Result.Failure(DispenseItemError.NotFound, "That item is no longer in the queue.");
        if (item.Status != DispenseStatus.Pending)
            return Result.Failure(DispenseItemError.AlreadyHandled, $"{item.Product.Name} for {item.Patient.Name} has already been {item.Status.ToString().ToLowerInvariant()}.");
        if (item.AwaitingAuthorisation)
            return Result.Failure(DispenseItemError.NeedsAuthorisation, "A vet must authorise this before it is dispensed.");

        string? witnessName = null;
        if (item.Product.IsControlledDrug)
        {
            var (witness, problem) = await WitnessCheck.VerifyAsync(_dbContext, _hasher, _user, request.WitnessId, request.WitnessPin, _clock.UtcNow, cancellationToken);
            if (problem is not null)
                return Result.Failure(DispenseItemError.WitnessRequired, problem);
            witnessName = witness!.ShortName;
        }

        if (StockLedger.CannotTake(item.Product, item.Quantity) is { } shortage)
            return Result.Failure(DispenseItemError.InsufficientStock, shortage);

        var now = _clock.UtcNow;
        var reference = $"{item.Patient.Name} ({item.Patient.PatientNumber})";
        StockLedger.Take(_dbContext, item.Product, item.Quantity, StockMovementReason.Dispensed, reference, _user.Name, now);
        if (item.Product.IsControlledDrug)
            StockLedger.Register(_dbContext, item.Product, ControlledDrugEntryKind.Supplied, -item.Quantity,
                $"{reference}, prescribed by {item.PrescribedBy}", _user.Name, witnessName, now);

        item.Status = DispenseStatus.Dispensed;
        item.DispensedUtc = now;
        item.DispensedBy = _user.Name;
        item.WitnessedBy = witnessName;

        if (item.Product.ReorderLevel > 0 && item.Product.OnHand <= item.Product.ReorderLevel)
            Notify.Add(_dbContext, now, $"{item.Product.Name} is at or below its reorder level ({item.Product.OnHand:0.##} {item.Product.Unit} left)",
                NotificationKind.Warning, "/pharmacy");

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(DispenseItemError.ChangedElsewhere, "Stock or the queue changed at another desk. Check it and try again.");
        return Result.Success();
    }
}
