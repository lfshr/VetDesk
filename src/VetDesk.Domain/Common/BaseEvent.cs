using MediatR;

namespace VetDesk.Domain.Common;

public abstract class BaseEvent : INotification
{
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
}
