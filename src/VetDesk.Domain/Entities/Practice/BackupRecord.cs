using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Practice;

public class BackupRecord : BaseEntity
{
    public DateTime StartedUtc { get; set; }
    public bool Succeeded { get; set; }
    public string? FilePath { get; set; }
    public string? Error { get; set; }
    public required string TriggeredBy { get; set; }
}
