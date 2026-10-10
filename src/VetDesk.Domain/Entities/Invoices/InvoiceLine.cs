using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Products;

namespace VetDesk.Domain.Entities.Invoices;

public class InvoiceLine : BaseEntity
{
    public Invoice Invoice { get; set; } = null!;
    public Product? Product { get; set; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public VatRate VatRate { get; set; }

    public decimal Net => Money.Round(Quantity * UnitPrice);
}
