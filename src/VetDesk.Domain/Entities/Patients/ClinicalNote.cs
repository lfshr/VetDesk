using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Patients;

/// <summary>A free note in the clinical history, such as a phone call with the owner.</summary>
public class ClinicalNote : BaseEntity
{
    public Patient Patient { get; set; } = null!;
    public DateTime WrittenUtc { get; set; }
    public required string Author { get; set; }
    public required string Title { get; set; }
    public string Body { get; set; } = string.Empty;
}
