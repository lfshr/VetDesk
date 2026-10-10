using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Products;

/// <summary>Every change to stock, so counts can be explained later.</summary>
public class StockMovement : BaseEntity
{
    public Product Product { get; set; } = null!;
    public StockBatch? Batch { get; set; }
    public decimal Change { get; set; }
    public StockMovementReason Reason { get; set; }
    public string? Reference { get; set; }
    public required string By { get; set; }
    public DateTime Utc { get; set; }
}

public enum StockMovementReason
{
    Received,
    Dispensed,
    StockTake
}
