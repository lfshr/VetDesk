using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Products;

namespace VetDesk.Domain.Entities.Consultations;

/// <summary>
/// An item on a consultation that will become an invoice line. Price and VAT rate are copied from the price list
/// when added so later price changes don't alter an open consultation. The price can be changed for this visit, and a
/// line can be free text with no product at all; either way it invoices exactly like a price-list line.
/// </summary>
public class ConsultationCharge : BaseEntity
{
    public Consultation Consultation { get; set; } = null!;
    /// <summary>Null for a free-text line: nothing to take off stock, dispense or put on the vaccination record.</summary>
    public Product? Product { get; set; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public VatRate VatRate { get; set; }

    /// <summary>Set when the vet prescribed this, so a dispensing task is raised on completion.</summary>
    public bool IsPrescription { get; set; }

    public string? Directions { get; set; }

    /// <summary>Why the vet went ahead despite a recorded allergy. Required to add a conflicting product.</summary>
    public string? AllergyOverrideReason { get; set; }

    /// <summary>The price list's unit price when someone charged a different price on this line; null otherwise.</summary>
    public decimal? ListUnitPrice { get; set; }

    /// <summary>Who changed the price. Kept for cash-up, never shown on the consultation or invoice.</summary>
    public string? PriceChangedBy { get; set; }

    /// <summary>Why the price is below the list price (required then).</summary>
    public string? PriceChangeReason { get; set; }

    public decimal Net => Money.Round(Quantity * UnitPrice);
}
