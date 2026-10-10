using VetDesk.Application.Appointments;
using VetDesk.Domain.Entities.Tasks;

namespace VetDesk.Application.Today;

public record FollowUpDto(int Id, string Text, int? PatientId, string? PatientName, DateOnly DueDate, bool IsDone, bool IsOverdue);

public record TodaySummary(
    DateOnly Date,
    TimeOnly? Open,
    TimeOnly? Close,
    IReadOnlyList<AppointmentDto> Appointments,
    int StillToSee,
    int WaitingNow,
    int SurgeryCount,
    int DoneCount,
    IReadOnlyList<FollowUpDto> FollowUps);

public record NotificationDto(int Id, DateTime CreatedUtc, string Text, NotificationKind Kind, string? Link, bool IsUnread);

public record NotificationFeed(IReadOnlyList<NotificationDto> Items, int UnreadCount);
