using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Appointments;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Appointments;
using SimpleResult;

namespace VetDesk.Application.Today.Queries;

public enum GetTodayError { NotSignedIn }

/// <summary>The Today screen: the day's list, the counts above it, and open follow-ups.</summary>
public class GetTodayQuery : IRequest<Result<TodaySummary>>
{
    /// <summary>Defaults to today in the practice's time zone.</summary>
    public DateOnly? Date { get; init; }
}

public class GetTodayQueryHandler : IRequestHandler<GetTodayQuery, Result<TodaySummary>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public GetTodayQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<TodaySummary>> Handle(GetTodayQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetTodayError.NotSignedIn, Access.NotSignedIn);

        var today = ClinicTime.Today(_clock.UtcNow);
        var date = request.Date ?? today;
        var (start, end) = ClinicTime.DayRange(date);

        var appointments = await AppointmentLoader.LoadAsync(_dbContext,
            q => q.Where(a => a.StartUtc >= start && a.StartUtc < end && a.Status != AppointmentStatus.Cancelled), cancellationToken);

        var followUps = await _dbContext.FollowUps.AsNoTracking().Include(f => f.Patient)
            .Where(f => (!f.IsDone && f.DueDate <= date) || (f.IsDone && f.DueDate == date))
            .OrderBy(f => f.IsDone).ThenBy(f => f.DueDate).ThenBy(f => f.Id)
            .ToListAsync(cancellationToken);

        var settings = await _dbContext.ClinicSettings.AsNoTracking().FirstAsync(cancellationToken);
        var hours = settings.HoursOn(date.DayOfWeek);

        return Result.Success(new TodaySummary(
            date,
            hours?.Open,
            hours?.Close,
            appointments,
            appointments.Count(a => a.Status is AppointmentStatus.Booked or AppointmentStatus.CheckedIn),
            appointments.Count(a => a.Status == AppointmentStatus.CheckedIn),
            appointments.Count(a => a.Kind == AppointmentKind.Surgery && a.Status != AppointmentStatus.NoShow),
            appointments.Count(a => a.Status == AppointmentStatus.Done),
            followUps.Select(f => new FollowUpDto(f.Id, f.Text, f.Patient?.Id, f.Patient?.Name, f.DueDate, f.IsDone, !f.IsDone && f.DueDate < today)).ToList()));
    }
}
