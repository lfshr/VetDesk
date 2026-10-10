using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Patients;

public class WeightRecord : BaseEntity
{
    public Patient Patient { get; set; } = null!;
    public DateTime RecordedUtc { get; set; }
    public decimal Kg { get; set; }

    /// <summary>What the weight was taken for, e.g. "Consultation" or "Dental (pre-GA)".</summary>
    public required string Context { get; set; }

    public string? RecordedBy { get; set; }
}
