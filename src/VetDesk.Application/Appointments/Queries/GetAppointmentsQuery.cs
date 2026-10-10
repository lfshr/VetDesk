using MediatR;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Appointments;
using SimpleResult;

namespace VetDesk.Application.Appointments.Queries;

public enum GetAppointmentsError { NotSignedIn, InvalidRange }

/// <summary>Appointments on the local calendar days From to To inclusive. Cancelled ones are left out unless asked for.</summary>
public class GetAppointmentsQuery : IRequest<Result<IReadOnlyList<AppointmentDto>>>
{
    internal const int MaxRangeDays = 42;

    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public bool IncludeCancelled { get; init; }
}

public class GetAppointmentsQueryHandler : IRequestHandler<GetAppointmentsQuery, Result<IReadOnlyList<AppointmentDto>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetAppointmentsQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<IReadOnlyList<AppointmentDto>>> Handle(GetAppointmentsQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetAppointmentsError.NotSignedIn, Access.NotSignedIn);
        if (request.To < request.From || request.To.DayNumber - request.From.DayNumber > GetAppointmentsQuery.MaxRangeDays)
            return Result.Failure(GetAppointmentsError.InvalidRange, $"Choose a range of at most {GetAppointmentsQuery.MaxRangeDays} days.");

        var start = ClinicTime.DayRange(request.From).StartUtc;
        var end = ClinicTime.DayRange(request.To).EndUtc;

        var appointments = await AppointmentLoader.LoadAsync(_dbContext, q =>
        {
            q = q.Where(a => a.StartUtc >= start && a.StartUtc < end);
            return request.IncludeCancelled ? q : q.Where(a => a.Status != AppointmentStatus.Cancelled);
        }, cancellationToken);

        return Result.Success(appointments);
    }
}
