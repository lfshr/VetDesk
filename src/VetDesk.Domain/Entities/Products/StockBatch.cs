using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Products;

public class StockBatch : BaseEntity, IHasVersion
{
    public Product Product { get; set; } = null!;
    public required string BatchNumber { get; set; }
    public DateOnly? Expiry { get; set; }
    public decimal Quantity { get; set; }
    public DateTime ReceivedUtc { get; set; }
    public Guid Version { get; set; }
}
