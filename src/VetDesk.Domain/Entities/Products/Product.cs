using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Products;

/// <summary>
/// An entry in the price list: a service (consultation, nail clip), a medicine, a vaccine or a retail item.
/// Stocked products also have batches in the pharmacy.
/// </summary>
public class Product : BaseAuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public ProductCategory Category { get; set; }

    /// <summary>Price per unit before VAT.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>What the practice pays per unit, before VAT. Used for stock valuation.</summary>
    public decimal CostPrice { get; set; }

    public VatRate VatRate { get; set; } = VatRate.Standard;

    /// <summary>Description of the pack shown under the name, e.g. "Injection · 5 ml".</summary>
    public string? Form { get; set; }

    /// <summary>What a quantity of 1 means: "ml", "tablet", "dose", "item".</summary>
    public string Unit { get; set; } = "item";

    public string? StockCategory { get; set; }
    public bool IsControlledDrug { get; set; }
    public string? DrugClass { get; set; }
    public string? Ingredients { get; set; }

    public bool IsStocked { get; set; }
    public decimal ReorderLevel { get; set; }
    public string? Location { get; set; }

    /// <summary>For vaccines: months until the next dose is due. Drives the vaccination record and reminders.</summary>
    public int? BoosterMonths { get; set; }

    public bool IsActive { get; set; } = true;

    public List<StockBatch> Batches { get; init; } = new();

    public decimal OnHand => Batches.Sum(b => b.Quantity);
}

public enum ProductCategory
{
    Service,
    Medicine,
    Vaccine,
    Retail
}
