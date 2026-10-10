using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Appointments;
using SimpleResult;

namespace VetDesk.Application.Appointments.Queries;

public enum GetBookingSlotsError { NotSignedIn, InvalidRequest }

/// <summary>Closed: outside the day's opening hours. Closed and lunch slots can still be booked; past ones can't.</summary>
public enum SlotState { Free, Booked, Lunch, Past, Closed }

public record BookingSlot(TimeOnly Time, DateTime StartUtc, SlotState State, int BookedCount);

public record BookingDay(DateOnly Date, bool IsClosed, TimeOnly? Open, TimeOnly? Close, IReadOnlyList<BookingSlot> Slots);

/// <summary>
/// The time grid in the booking dialog: every 15 minutes across the weekday hours (wider if the day's own hours are),
/// with how busy each one is. Out-of-hours times are marked Closed rather than left out, so staff can still book an
/// out-of-hours visit, as the design shows.
/// </summary>
public class GetBookingSlotsQuery : IRequest<Result<BookingDay>>
{
    public DateOnly Date { get; init; }
    public int Minutes { get; init; } = 15;
    public int? IgnoreAppointmentId { get; init; }
}

public class GetBookingSlotsQueryHandler : IRequestHandler<GetBookingSlotsQuery, Result<BookingDay>>
{
    internal const int SlotMinutes = 15;

    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public GetBookingSlotsQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<BookingDay>> Handle(GetBookingSlotsQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetBookingSlotsError.NotSignedIn, Access.NotSignedIn);
        if (request.Minutes <= 0)
            return Result.Failure(GetBookingSlotsError.InvalidRequest, "Choose a length.");

        var settings = await _dbContext.ClinicSettings.AsNoTracking().FirstAsync(cancellationToken);
        var hours = settings.HoursOn(request.Date.DayOfWeek);
        var gridOpen = hours is { } h && h.Open < settings.WeekdayOpen ? h.Open : settings.WeekdayOpen;
        var gridClose = hours is { } c && c.Close > settings.WeekdayClose ? c.Close : settings.WeekdayClose;
        var lunch = settings.LunchOn(request.Date.DayOfWeek);
        var (dayStart, dayEnd) = ClinicTime.DayRange(request.Date);
        var booked = await _dbContext.Appointments.AsNoTracking()
            .Where(a => a.StartUtc >= dayStart && a.StartUtc < dayEnd && a.Id != request.IgnoreAppointmentId)
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
            .Select(a => new { a.StartUtc, a.Minutes })
            .ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        var slots = new List<BookingSlot>();
        for (var time = gridOpen; time < gridClose && time >= gridOpen; time = time.AddMinutes(SlotMinutes))
        {
            var startUtc = ClinicTime.ToUtc(request.Date, time);
            var endUtc = startUtc.AddMinutes(request.Minutes);
            var count = booked.Count(b => Utc.Mark(b.StartUtc) < endUtc && startUtc < Utc.Mark(b.StartUtc).AddMinutes(b.Minutes));
            var state = startUtc < now ? SlotState.Past
                : hours is not { } open || time < open.Open || time >= open.Close ? SlotState.Closed
                : lunch is { } l && time >= l.Start && time < l.End ? SlotState.Lunch
                : count > 0 ? SlotState.Booked
                : SlotState.Free;
            slots.Add(new BookingSlot(time, startUtc, state, count));
        }

        return Result.Success(new BookingDay(request.Date, hours is null, hours?.Open, hours?.Close, slots));
    }
}
