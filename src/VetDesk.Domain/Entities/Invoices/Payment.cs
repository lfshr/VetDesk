using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Invoices;

/// <summary>Money taken against an invoice. A refund is a payment with a negative amount.</summary>
public class Payment : BaseEntity
{
    public Invoice Invoice { get; set; } = null!;
    public DateTime TakenUtc { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Card terminal, bank transfer reference or refund reason.</summary>
    public string? Reference { get; set; }

    public required string TakenBy { get; set; }

    /// <summary>The cash-up that counted this payment. Null until the day is closed.</summary>
    public int? DayCloseId { get; set; }
    public DayClose? DayClose { get; set; }

    public bool IsRefund => Amount < 0;
}

public enum PaymentMethod
{
    Card,
    Cash,
    Transfer
}
