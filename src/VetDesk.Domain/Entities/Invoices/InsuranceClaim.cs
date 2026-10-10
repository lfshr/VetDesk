using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Invoices;

/// <summary>
/// The details of an insurance claim. In v1 the claim form is sent outside VetDesk; this records that it was sent.
/// </summary>
public class InsuranceClaim : BaseEntity
{
    public Invoice Invoice { get; set; } = null!;
    public required string Insurer { get; set; }
    public required string PolicyNumber { get; set; }
    public decimal Excess { get; set; }
    public required string Condition { get; set; }
    public DateTime CreatedUtc { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Sent;
}

public enum ClaimStatus
{
    Sent,
    Settled,
    Rejected
}
