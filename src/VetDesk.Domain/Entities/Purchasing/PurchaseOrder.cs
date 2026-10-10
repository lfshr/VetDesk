using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Products;

namespace VetDesk.Domain.Entities.Purchasing;

public class PurchaseOrder : BaseAuditableEntity, IHasVersion
{
    public required string Number { get; set; }
    public required string Supplier { get; set; }
    public DateOnly? DeliverBy { get; set; }
    public string? Note { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime? SentUtc { get; set; }
    public DateTime? ReceivedUtc { get; set; }
    public List<PurchaseOrderLine> Lines { get; init; } = new();
    public Guid Version { get; set; }

    public bool HasControlledDrugs => Lines.Any(l => l.Product.IsControlledDrug);

    public static string FormatNumber(int sequence) => $"PO-{sequence:D4}";
}

public class PurchaseOrderLine : BaseEntity
{
    public PurchaseOrder Order { get; set; } = null!;
    public required Product Product { get; set; }
    public decimal Quantity { get; set; }
}

public enum PurchaseOrderStatus
{
    Draft,
    Sent,
    Received
}
