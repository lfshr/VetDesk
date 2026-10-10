using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Patients;

namespace VetDesk.Domain.Entities.Invoices;

/// <summary>
/// A bill to a client. Totals are calculated once, when the invoice is raised, and stored, so the figures a client
/// was given never change. Corrections are made with refunds, not by editing lines.
/// </summary>
public class Invoice : BaseAuditableEntity, IHasVersion
{
    public required string Number { get; set; }
    public required Client Client { get; set; }
    public Patient? Patient { get; set; }
    public int? ConsultationId { get; set; }
    public DateTime IssuedUtc { get; set; }
    public DateOnly DueDate { get; set; }

    public List<InvoiceLine> Lines { get; init; } = new();
    public List<Payment> Payments { get; init; } = new();
    public InsuranceClaim? Claim { get; set; }

    public decimal Net { get; private set; }
    public decimal Vat { get; private set; }
    public decimal Total { get; private set; }

    public Guid Version { get; set; }

    /// <summary>Money received less money refunded.</summary>
    public decimal Paid => Payments.Sum(p => p.Amount);

    /// <summary>Money received, before any refunds.</summary>
    public decimal Received => Payments.Where(p => p.Amount > 0).Sum(p => p.Amount);

    public decimal Refunded => -Payments.Where(p => p.Amount < 0).Sum(p => p.Amount);

    /// <summary>
    /// A refund lowers what is owed by as much as it hands back (a goodwill refund on a paid bill doesn't put the client
    /// back in debt), so only money never received is due.
    /// </summary>
    public decimal AmountDue => Math.Max(0, Total - Received);

    public void SetTotals(VatBreakdown totals)
    {
        Net = totals.Net;
        Vat = totals.Vat;
        Total = totals.Total;
    }

    public InvoiceStatus StatusOn(DateOnly today)
    {
        if (Refunded > 0 && Paid <= 0)
            return InvoiceStatus.Refunded;
        if (Total > 0 && Received >= Total)
            return InvoiceStatus.Paid;
        if (Total == 0)
            return InvoiceStatus.Paid;
        if (Claim is { Status: ClaimStatus.Sent })
            return InvoiceStatus.ClaimSent;
        if (Paid > 0)
            return InvoiceStatus.PartPaid;
        return DueDate < today ? InvoiceStatus.Overdue : InvoiceStatus.Unpaid;
    }

    public static string FormatNumber(int sequence) => $"INV-{sequence:D5}";
}

public enum InvoiceStatus
{
    Unpaid,
    Overdue,
    PartPaid,
    ClaimSent,
    Paid,
    Refunded
}
