using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Consultations;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Consultations.Commands;

public enum StartConsultationError { NotAllowed, PatientNotFound, AppointmentNotFound, InvalidRequest, ChangedElsewhere }

/// <summary>
/// Opens a consultation for a patient, from an appointment or as a walk-in. If one is already open for the same
/// appointment, that one is returned so two clicks don't create two records.
/// </summary>
public class StartConsultationCommand : IRequest<Result<int>>
{
    public int PatientId { get; init; }
    public int? AppointmentId { get; init; }
}

public class StartConsultationCommandHandler : IRequestHandler<StartConsultationCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public StartConsultationCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<int>> Handle(StartConsultationCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(StartConsultationError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));

        var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure(StartConsultationError.PatientNotFound, "That patient no longer exists.");
        if (patient.IsDeceased)
            return Result.Failure(StartConsultationError.InvalidRequest, $"{patient.Name} is marked as deceased.");

        Appointment? appointment = null;
        if (request.AppointmentId is { } appointmentId)
        {
            appointment = await _dbContext.Appointments.Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);
            if (appointment is null)
                return Result.Failure(StartConsultationError.AppointmentNotFound, "That appointment no longer exists.");
            if (appointment.Patient.Id != patient.Id)
                return Result.Failure(StartConsultationError.InvalidRequest, "That appointment is for a different patient.");

            var open = await _dbContext.Consultations
                .Where(c => c.Appointment != null && c.Appointment.Id == appointmentId && c.Status == ConsultationStatus.Open)
                .Select(c => (int?)c.Id).FirstOrDefaultAsync(cancellationToken);
            if (open is { } openId)
                return Result.Success(openId);

            if (!appointment.ChangeStatus(AppointmentStatus.InConsult, _clock.UtcNow))
                return Result.Failure(StartConsultationError.InvalidRequest, "That appointment is already finished or cancelled.");
        }

        var clinician = await _dbContext.StaffMembers.FirstAsync(s => s.Id == _user.StaffId, cancellationToken);
        var consultation = new Consultation
        {
            Patient = patient,
            Appointment = appointment,
            Clinician = clinician,
            StartedUtc = _clock.UtcNow,
            Reason = appointment?.Reason,
        };
        _dbContext.Consultations.Add(consultation);

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(StartConsultationError.ChangedElsewhere, Saving.ChangedElsewhere);
        return Result.Success(consultation.Id);
    }
}
