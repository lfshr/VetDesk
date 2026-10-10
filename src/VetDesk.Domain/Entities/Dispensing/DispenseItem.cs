using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Consultations;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Products;

namespace VetDesk.Domain.Entities.Dispensing;

/// <summary>A medicine waiting in the pharmacy's "To dispense" queue.</summary>
public class DispenseItem : BaseAuditableEntity, IHasVersion
{
    public required Patient Patient { get; set; }
    public required Product Product { get; set; }
    public Consultation? Consultation { get; set; }
    public decimal Quantity { get; set; }
    public string? Directions { get; set; }
    public required string PrescribedBy { get; set; }

    /// <summary>Raised by someone who can't prescribe (e.g. a repeat request at reception): a vet must authorise it.</summary>
    public bool RequiresAuthorisation { get; set; }
    public string? AuthorisedBy { get; set; }

    public DispenseStatus Status { get; set; } = DispenseStatus.Pending;
    public DateTime? DispensedUtc { get; set; }
    public string? DispensedBy { get; set; }
    public string? WitnessedBy { get; set; }

    public Guid Version { get; set; }

    public bool AwaitingAuthorisation => RequiresAuthorisation && AuthorisedBy is null;
}

public enum DispenseStatus
{
    Pending,
    Dispensed,
    Cancelled
}
