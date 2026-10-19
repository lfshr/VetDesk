using VetDesk.Application.Common;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Consultations;

namespace VetDesk.Application.Consultations;

/// <summary>
/// Charging a different price on one consultation line. The price list doesn't change; the line remembers the list
/// price, who changed it and, when it's lower, why, for cash-up. Free-text lines have no list price to compare with.
/// </summary>
internal static class ChargePrice
{
    /// <summary>Sets <paramref name="charge"/>'s net unit price. Returns why it can't, or null.</summary>
    public static string? Set(ConsultationCharge charge, decimal unitPrice, string? reason, string changedBy)
    {
        if (unitPrice < 0)
            return "The price can't be less than zero.";
        unitPrice = Money.Round(unitPrice);

        var list = charge.ListUnitPrice ?? (charge.Product is null ? null : charge.UnitPrice);
        if (list is not { } listPrice || unitPrice == listPrice)
        {
            charge.UnitPrice = unitPrice;
            charge.ListUnitPrice = null;
            charge.PriceChangedBy = charge.PriceChangeReason = null;
            return null;
        }

        reason = Text.Clean(reason);
        if (unitPrice < listPrice && reason is null)
            return "Give a reason for charging less than the list price.";

        charge.UnitPrice = unitPrice;
        charge.ListUnitPrice = listPrice;
        charge.PriceChangedBy = changedBy;
        charge.PriceChangeReason = unitPrice < listPrice ? reason : null;
        return null;
    }
}
