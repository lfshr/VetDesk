using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Invoices;

/// <summary>An end-of-day cash-up. Closing the day stamps every payment it counted.</summary>
public class DayClose : BaseEntity
{
    public DateOnly Date { get; set; }
    public DateTime ClosedUtc { get; set; }
    public required string ClosedBy { get; set; }

    public decimal CardTotal { get; set; }
    public decimal CashTotal { get; set; }
    public decimal TransferTotal { get; set; }

    public decimal OpeningFloat { get; set; }
    public decimal CashCounted { get; set; }

    public decimal ExpectedCash => OpeningFloat + CashTotal;
    public decimal Variance => CashCounted - ExpectedCash;
    public string? Note { get; set; }
}
