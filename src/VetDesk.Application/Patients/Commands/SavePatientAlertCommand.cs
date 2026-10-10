using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Patients.Commands;

public enum SavePatientAlertError { NotAllowed, InvalidRequest, NotFound }

/// <summary>Adds or edits an alert chip. Picking a preset fills in its text and severity.</summary>
public class SavePatientAlertCommand : IRequest<Result<int>>
{
    public int PatientId { get; init; }
    public int? AlertId { get; init; }
    public string? PresetKey { get; init; }
    public string Text { get; init; } = string.Empty;
    public AlertSeverity Severity { get; init; }
}

public class SavePatientAlertCommandHandler : IRequestHandler<SavePatientAlertCommand, Result<int>>
{
    internal const int MaxLength = 120;

    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public SavePatientAlertCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<int>> Handle(SavePatientAlertCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(SavePatientAlertError.NotAllowed, Access.Denied(Permission.ViewClinicalRecords));

        var preset = PatientAlert.Presets.FirstOrDefault(p => p.Key == request.PresetKey);
        var text = preset.Text ?? request.Text.Trim();
        if (text.Length == 0)
            return Result.Failure(SavePatientAlertError.InvalidRequest, "Choose an alert or type one.");
        if (text.Length > MaxLength)
            return Result.Failure(SavePatientAlertError.InvalidRequest, $"Keep alerts under {MaxLength} characters.");

        var patient = await _dbContext.Patients.Include(p => p.Alerts).FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure(SavePatientAlertError.NotFound, "That patient no longer exists.");

        PatientAlert alert;
        if (request.AlertId is { } alertId)
        {
            var existing = patient.Alerts.FirstOrDefault(a => a.Id == alertId);
            if (existing is null)
                return Result.Failure(SavePatientAlertError.NotFound, "That alert has already been removed.");
            alert = existing;
            alert.Text = text;
        }
        else
        {
            alert = new PatientAlert { Text = text, Patient = patient };
            patient.Alerts.Add(alert);
        }

        alert.Severity = preset.Text is null ? request.Severity : preset.Severity;
        alert.PresetKey = preset.Text is null ? null : preset.Key;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(alert.Id);
    }
}
