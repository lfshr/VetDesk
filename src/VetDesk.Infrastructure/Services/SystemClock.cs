using VetDesk.Application.Common.Interfaces;

namespace VetDesk.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
