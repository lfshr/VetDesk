using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Purchasing;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Domain.Entities.Tasks;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Commands;

public enum ReceiveDeliveryError { NotAllowed, InvalidRequest, OrderNotFound, ProductNotFound, ChangedElsewhere }

public record ReceivedLine(int ProductId, decimal Quantity, string BatchNumber, DateOnly? Expiry);

/// <summary>Books a delivery into stock by batch. Controlled drugs received are entered in the register.</summary>
public class ReceiveDeliveryCommand : IRequest<Result>
{
    public int? OrderId { get; init; }
    public IReadOnlyList<ReceivedLine> Lines { get; init; } = [];
}

public class ReceiveDeliveryCommandHandler : IRequestHandler<ReceiveDeliveryCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public ReceiveDeliveryCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result> Handle(ReceiveDeliveryCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(ReceiveDeliveryError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));

        var lines = request.Lines.Where(l => l.Quantity > 0).ToList();
        if (lines.Count == 0)
            return Result.Failure(ReceiveDeliveryError.InvalidRequest, "Enter the quantity received for at least one item.");
        if (lines.Any(l => !Text.Has(l.BatchNumber)))
            return Result.Failure(ReceiveDeliveryError.InvalidRequest, "Enter a batch number for each item received.");

        PurchaseOrder? order = null;
        if (request.OrderId is { } orderId)
        {
            order = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (order is null || order.Status != PurchaseOrderStatus.Sent)
                return Result.Failure(ReceiveDeliveryError.OrderNotFound, "That order isn't waiting for delivery.");
        }

        var ids = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products.Include(p => p.Batches).Where(p => ids.Contains(p.Id) && p.IsStocked)
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        if (products.Count != ids.Count)
            return Result.Failure(ReceiveDeliveryError.ProductNotFound, "One of the items isn't a stocked product.");

        var now = _clock.UtcNow;
        var reference = order is null ? "Delivery" : $"Delivery {order.Number} from {order.Supplier}";
        foreach (var line in lines)
        {
            var product = products[line.ProductId];
            StockLedger.Receive(_dbContext, product, line.BatchNumber.Trim().ToUpperInvariant(), line.Expiry, line.Quantity, reference, _user.Name, now);
            if (product.IsControlledDrug)
                StockLedger.Register(_dbContext, product, ControlledDrugEntryKind.Received, line.Quantity,
                    $"{reference}, batch {line.BatchNumber.Trim().ToUpperInvariant()}", _user.Name, null, now);
        }

        if (order is not null)
        {
            order.Status = PurchaseOrderStatus.Received;
            order.ReceivedUtc = now;
        }

        Notify.Add(_dbContext, now, $"{reference} received ({lines.Count} item{(lines.Count == 1 ? "" : "s")})", NotificationKind.Info, "/pharmacy");

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(ReceiveDeliveryError.ChangedElsewhere, "Stock changed at another desk. Try again.");
        return Result.Success();
    }
}
