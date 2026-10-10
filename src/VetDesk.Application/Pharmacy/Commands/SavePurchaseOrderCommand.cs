using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Invoices;
using VetDesk.Domain.Entities.Practice;
using VetDesk.Domain.Entities.Purchasing;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Commands;

public enum SavePurchaseOrderError { NotAllowed, InvalidRequest, ProductNotFound, ChangedElsewhere }

public record OrderLine(int ProductId, decimal Quantity);

/// <summary>
/// "Save draft" or "Send". Sending marks the order as on its way so it shows under Receive delivery; the order
/// itself is sent to the supplier outside VetDesk in v1 (no wholesaler integration).
/// </summary>
public class SavePurchaseOrderCommand : IRequest<Result<string>>
{
    public string Supplier { get; init; } = string.Empty;
    public DateOnly? DeliverBy { get; init; }
    public string? Note { get; init; }
    public IReadOnlyList<OrderLine> Lines { get; init; } = [];
    public bool Send { get; init; }
}

public class SavePurchaseOrderCommandHandler : IRequestHandler<SavePurchaseOrderCommand, Result<string>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public SavePurchaseOrderCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<string>> Handle(SavePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(SavePurchaseOrderError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));
        if (!Text.Has(request.Supplier))
            return Result.Failure(SavePurchaseOrderError.InvalidRequest, "Choose a supplier.");

        var lines = request.Lines.Where(l => l.Quantity > 0).ToList();
        if (lines.Count == 0)
            return Result.Failure(SavePurchaseOrderError.InvalidRequest, "Add at least one item.");

        var ids = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products.Where(p => ids.Contains(p.Id) && p.IsStocked).ToDictionaryAsync(p => p.Id, cancellationToken);
        if (products.Count != ids.Count)
            return Result.Failure(SavePurchaseOrderError.ProductNotFound, "One of the items isn't a stocked product.");

        var sequence = await InvoiceFactory.NextSequenceAsync(_dbContext, NumberSequence.PurchaseOrder, cancellationToken);
        var order = new PurchaseOrder
        {
            Number = PurchaseOrder.FormatNumber(sequence.Take()),
            Supplier = request.Supplier.Trim(),
            DeliverBy = request.DeliverBy,
            Note = Text.Clean(request.Note),
            Status = request.Send ? PurchaseOrderStatus.Sent : PurchaseOrderStatus.Draft,
            SentUtc = request.Send ? _clock.UtcNow : null,
        };
        order.Lines.AddRange(lines.Select(l => new PurchaseOrderLine { Order = order, Product = products[l.ProductId], Quantity = l.Quantity }));
        _dbContext.PurchaseOrders.Add(order);

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(SavePurchaseOrderError.ChangedElsewhere, "Another order was saved at the same moment. Try again.");
        return Result.Success(order.Number);
    }
}
