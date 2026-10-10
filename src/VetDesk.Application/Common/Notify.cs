using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Tasks;

namespace VetDesk.Application.Common;

internal static class Notify
{
    /// <summary>Queues a bell notification; it is saved with the caller's other changes.</summary>
    public static void Add(IVetDeskDbContext db, DateTime nowUtc, string text, NotificationKind kind = NotificationKind.Info, string? link = null) =>
        db.Notifications.Add(new Notification { CreatedUtc = nowUtc, Text = text, Kind = kind, Link = link });
}
