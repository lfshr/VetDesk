using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Practice;

/// <summary>Hands out gap-free numbers, such as invoice numbers, which HMRC expects to run in sequence.</summary>
public class NumberSequence : BaseEntity, IHasVersion
{
    public required string Name { get; init; }
    public int Next { get; set; } = 1;
    public Guid Version { get; set; }

    public int Take() => Next++;

    public const string Invoice = "Invoice";
    public const string PurchaseOrder = "PurchaseOrder";
}
