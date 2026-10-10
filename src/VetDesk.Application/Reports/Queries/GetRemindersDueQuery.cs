using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using SimpleResult;

namespace VetDesk.Application.Reports.Queries;

public enum GetRemindersDueError { NotSignedIn }

/// <summary>
/// Vaccinations falling due in the next few weeks (and overdue ones), latest dose per vaccine only. Text and email
/// sending aren't connected in v1, so the screen exports this list for the practice to send.
/// </summary>
public class GetRemindersDueQuery : IRequest<Result<IReadOnlyList<ReminderDue>>>
{
    public int Days { get; init; } = 30;
    public int OverdueDays { get; init; } = 60;
}

public class GetRemindersDueQueryHandler : IRequestHandler<GetRemindersDueQuery, Result<IReadOnlyList<ReminderDue>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public GetRemindersDueQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<ReminderDue>>> Handle(GetRemindersDueQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetRemindersDueError.NotSignedIn, Access.NotSignedIn);

        var today = ClinicTime.Today(_clock.UtcNow);
        var until = today.AddDays(request.Days);
        var since = today.AddDays(-request.OverdueDays);

        var vaccinations = await _dbContext.Vaccinations.AsNoTracking()
            .Include(v => v.Patient).ThenInclude(p => p.Client)
            .Where(v => !v.Patient.IsDeceased && v.DueDate != null)
            .ToListAsync(cancellationToken);

        IReadOnlyList<ReminderDue> due = vaccinations
            .GroupBy(v => (v.Patient.Id, v.VaccineName))
            .Select(g => g.OrderByDescending(v => v.GivenUtc).First())
            .Where(v => v.DueDate >= since && v.DueDate <= until)
            .OrderBy(v => v.DueDate)
            .Select(v => new ReminderDue(v.Patient.Id, v.Patient.Name, v.Patient.Client.FullName, v.Patient.Client.Phone, v.Patient.Client.Email,
                v.VaccineName, v.DueDate!.Value, v.Patient.Client.RemindersOptIn))
            .ToList();
        return Result.Success(due);
    }
}
