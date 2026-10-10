using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Tasks;

/// <summary>A line in the bell menu, e.g. "Biscuit checked in" or "Methadone below reorder level".</summary>
public class Notification : BaseEntity
{
    public DateTime CreatedUtc { get; set; }
    public required string Text { get; set; }
    public NotificationKind Kind { get; set; }

    /// <summary>App route to open when clicked, e.g. "/patients/12".</summary>
    public string? Link { get; set; }
}

public enum NotificationKind
{
    Info,
    Success,
    Warning
}
