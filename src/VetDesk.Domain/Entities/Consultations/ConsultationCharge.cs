using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Products;

namespace VetDesk.Domain.Entities.Consultations;

/// <summary>
/// An item on a consultation that will become an invoice line. Price and VAT rate are copied from the price list
/// when added so later price changes don't alter an open consultation.
/// </summary>
public class ConsultationCharge : BaseEntity
{
    public Consultation Consultation { get; set; } = null!;
    public required Product Product { get; set; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public VatRate VatRate { get; set; }

    /// <summary>Set when the vet prescribed this, so a dispensing task is raised on completion.</summary>
    public bool IsPrescription { get; set; }

    public string? Directions { get; set; }

    /// <summary>Why the vet went ahead despite a recorded allergy. Required to add a conflicting product.</summary>
    public string? AllergyOverrideReason { get; set; }

    public decimal Net => Money.Round(Quantity * UnitPrice);
}
