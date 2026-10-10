using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Products;

/// <summary>
/// A line in the controlled drugs register. Entries are never edited or deleted; corrections are new entries.
/// </summary>
public class ControlledDrugEntry : BaseEntity
{
    public Product Product { get; set; } = null!;
    public DateTime Utc { get; set; }
    public ControlledDrugEntryKind Kind { get; set; }

    /// <summary>Amount in the product's unit: positive in, negative out, zero for a balance check.</summary>
    public decimal Change { get; set; }

    /// <summary>Stock held after this entry.</summary>
    public decimal Balance { get; set; }

    public required string Reason { get; set; }
    public required string By { get; set; }
    public string? Witness { get; set; }
}

public enum ControlledDrugEntryKind
{
    Received,
    Supplied,
    BalanceCheck,
    Adjustment
}
