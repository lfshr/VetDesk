using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Patients;

namespace VetDesk.Domain.Entities.Tasks;

/// <summary>A to-do on the Today screen: call an owner back, chase lab results, send a reminder.</summary>
public class FollowUp : BaseAuditableEntity
{
    public required string Text { get; set; }
    public Patient? Patient { get; set; }
    public DateOnly DueDate { get; set; }
    public bool IsDone { get; set; }
    public DateTime? DoneUtc { get; set; }
    public string? DoneBy { get; set; }
}
