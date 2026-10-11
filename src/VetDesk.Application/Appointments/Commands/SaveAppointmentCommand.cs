using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Appointments;
using SimpleResult;

namespace VetDesk.Application.Appointments.Commands;

public enum SaveAppointmentError
{
    NotSignedIn, InvalidRequest, NotFound, PatientNotFound, TypeNotFound, ClinicianNotFound, Clash, NotEditable, ChangedElsewhere,

    /// <summary>A closed-day, out-of-hours or busy time booked by someone who can't book it alone: ask a vet or administrator.</summary>
    NeedsApproval,

    /// <summary>An approver was given but can't approve (wrong PIN, wrong role, locked out).</summary>
    ApprovalRefused
}

/// <summary>Books a new appointment (Id null) or changes an existing one from the appointment dialog.</summary>
public class SaveAppointmentCommand : IRequest<Result<int>>
{
    internal const int MaxMinutes = 8 * 60;

    public int? Id { get; init; }
    public int PatientId { get; init; }
    public int TypeId { get; init; }
    public int? ClinicianId { get; init; }
    public DateTime StartUtc { get; init; }
    public int Minutes { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool SmsReminder { get; init; }
    public Guid? Version { get; init; }

    /// <summary>The vet or administrator approving a closed-day, out-of-hours or busy time, with their PIN.</summary>
    public int? ApproverId { get; init; }
    public string? ApproverPin { get; init; }
}

public class SaveAppointmentCommandHandler : IRequestHandler<SaveAppointmentCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IPasswordHasher _hasher;
    private readonly IDevicePinLimit _deviceLimit;

    public SaveAppointmentCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock, IPasswordHasher hasher, IDevicePinLimit deviceLimit)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
        _hasher = hasher;
        _deviceLimit = deviceLimit;
    }

    public async Task<Result<int>> Handle(SaveAppointmentCommand request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(SaveAppointmentError.NotSignedIn, Access.NotSignedIn);
        if (!Text.Has(request.Reason))
            return Result.Failure(SaveAppointmentError.InvalidRequest, "Enter a reason for the visit.");
        if (request.Minutes <= 0 || request.Minutes > SaveAppointmentCommand.MaxMinutes)
            return Result.Failure(SaveAppointmentError.InvalidRequest, "Choose a length.");
        if (request.StartUtc.Kind != DateTimeKind.Utc)
            return Result.Failure(SaveAppointmentError.InvalidRequest, "The start time must be given in UTC.");

        var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure(SaveAppointmentError.PatientNotFound, "Choose a patient.");
        if (patient.IsDeceased)
            return Result.Failure(SaveAppointmentError.InvalidRequest, $"{patient.Name} is marked as deceased.");

        var type = await _dbContext.AppointmentTypes.FirstOrDefaultAsync(t => t.Id == request.TypeId, cancellationToken);
        if (type is null)
            return Result.Failure(SaveAppointmentError.TypeNotFound, "Choose an appointment type.");

        var clinician = request.ClinicianId is { } clinicianId
            ? await _dbContext.StaffMembers.FirstOrDefaultAsync(s => s.Id == clinicianId && s.IsActive, cancellationToken)
            : null;
        if (request.ClinicianId is not null && clinician is null)
            return Result.Failure(SaveAppointmentError.ClinicianNotFound, "That clinician isn't available.");

        var endUtc = request.StartUtc.AddMinutes(request.Minutes);
        if (await AppointmentClashes.FindAsync(_dbContext, patient.Id, clinician?.Id, request.StartUtc, endUtc, request.Id, cancellationToken) is { } clash)
            return Result.Failure(SaveAppointmentError.Clash, clash);

        Appointment? existing = null;
        if (request.Id is { } id)
        {
            existing = await _dbContext.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (existing is null)
                return Result.Failure(SaveAppointmentError.NotFound, "That appointment no longer exists.");
            if (request.Version is { } version && existing.Version != version)
                return Result.Failure(SaveAppointmentError.ChangedElsewhere, Saving.ChangedElsewhere);
            if (!existing.IsOpen)
                return Result.Failure(SaveAppointmentError.NotEditable, "Finished or cancelled appointments can't be changed.");
        }

        // Only a new time needs approving: changing the reason or type of an approved booking keeps its approver.
        var approvedBy = existing?.ApprovedBy;
        if (existing is null || existing.StartUtc != request.StartUtc || existing.Minutes != request.Minutes)
        {
            approvedBy = null;
            if (await BookingApproval.WhyNeededAsync(_dbContext, request.StartUtc, request.Minutes, request.Id, cancellationToken) is { } why)
            {
                var booker = await _dbContext.StaffMembers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == _user.StaffId, cancellationToken);
                if (booker?.CanApproveBookings != true)
                {
                    if (request.ApproverId is not { } approverId)
                        return Result.Failure(SaveAppointmentError.NeedsApproval, $"{why} {BookingApproval.Ask}");
                    var (approver, problem) = await BookingApproval.VerifyAsync(
                        _dbContext, _hasher, _deviceLimit, _user, approverId, request.ApproverPin, _clock.UtcNow, cancellationToken);
                    if (problem is not null)
                        return Result.Failure(SaveAppointmentError.ApprovalRefused, problem);
                    approvedBy = approver!.ShortName;
                }
            }
        }

        Appointment appointment;
        if (existing is not null)
        {
            appointment = existing;
            appointment.Patient = patient;
            appointment.Type = type;
            appointment.Reason = request.Reason.Trim();
        }
        else
        {
            appointment = new Appointment { Patient = patient, Type = type, Reason = request.Reason.Trim() };
            _dbContext.Appointments.Add(appointment);
        }

        appointment.ApprovedBy = approvedBy;
        appointment.Clinician = clinician;
        appointment.StartUtc = request.StartUtc;
        appointment.Minutes = request.Minutes;
        appointment.SmsReminder = request.SmsReminder;

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(SaveAppointmentError.ChangedElsewhere, Saving.ChangedElsewhere);
        return Result.Success(appointment.Id);
    }
}
