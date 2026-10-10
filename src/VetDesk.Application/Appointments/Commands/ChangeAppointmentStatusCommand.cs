using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Tasks;
using SimpleResult;

namespace VetDesk.Application.Appointments.Commands;

public enum ChangeAppointmentStatusError { NotSignedIn, NotFound, InvalidTransition, ChangedElsewhere }

/// <summary>
/// Check in, mark no-show, cancel, or undo a check-in. Starting and finishing a consultation move the appointment
/// too, but through the consultation commands.
/// </summary>
public class ChangeAppointmentStatusCommand : IRequest<Result>
{
    public int Id { get; init; }
    public AppointmentStatus Status { get; init; }
}

public class ChangeAppointmentStatusCommandHandler : IRequestHandler<ChangeAppointmentStatusCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public ChangeAppointmentStatusCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result> Handle(ChangeAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(ChangeAppointmentStatusError.NotSignedIn, Access.NotSignedIn);
        if (request.Status is AppointmentStatus.InConsult or AppointmentStatus.Done)
            return Result.Failure(ChangeAppointmentStatusError.InvalidTransition, "Start or complete the consultation instead.");

        var appointment = await _dbContext.Appointments.Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (appointment is null)
            return Result.Failure(ChangeAppointmentStatusError.NotFound, "That appointment no longer exists.");
        if (appointment.Status == request.Status)
            return Result.Success();

        var from = appointment.Status;
        if (!appointment.ChangeStatus(request.Status, _clock.UtcNow))
            return Result.Failure(ChangeAppointmentStatusError.InvalidTransition,
                $"An appointment that is {Describe(from)} can't be marked {Describe(request.Status)}.");

        if (request.Status == AppointmentStatus.CheckedIn)
            Notify.Add(_dbContext, _clock.UtcNow, $"{appointment.Patient.Name} checked in for {appointment.Reason.ToLowerInvariant()}",
                NotificationKind.Success, $"/patients/{appointment.Patient.Id}");

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(ChangeAppointmentStatusError.ChangedElsewhere, Saving.ChangedElsewhere);
        return Result.Success();
    }

    internal static string Describe(AppointmentStatus status) => status switch
    {
        AppointmentStatus.CheckedIn => "checked in",
        AppointmentStatus.InConsult => "in consultation",
        AppointmentStatus.NoShow => "a no-show",
        _ => status.ToString().ToLowerInvariant()
    };
}
