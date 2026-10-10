using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Consultations.Commands;

public enum SaveConsultationNotesError { NotAllowed, NotFound, Locked, InvalidRequest, ChangedElsewhere }

/// <summary>"Save draft": SOAP notes and vitals. Refused once the consultation is completed.</summary>
public class SaveConsultationNotesCommand : IRequest<Result<Guid>>
{
    public int Id { get; init; }
    public string? Reason { get; init; }
    public string? Subjective { get; init; }
    public string? Objective { get; init; }
    public string? Assessment { get; init; }
    public string? Plan { get; init; }
    public decimal? TemperatureC { get; init; }
    public int? HeartRate { get; init; }
    public int? RespiratoryRate { get; init; }
    public decimal? WeightKg { get; init; }
    public string? MucousMembranes { get; init; }
    public string? CapillaryRefill { get; init; }
    public Guid Version { get; init; }
}

public class SaveConsultationNotesCommandHandler : IRequestHandler<SaveConsultationNotesCommand, Result<Guid>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public SaveConsultationNotesCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<Guid>> Handle(SaveConsultationNotesCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(SaveConsultationNotesError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));

        // Wide limits that only catch typing slips (38.5 typed as 385), not clinical judgement.
        if (request.TemperatureC is < 25 or > 45)
            return Result.Failure(SaveConsultationNotesError.InvalidRequest, "Temperature should be in °C, between 25 and 45.");
        if (request.HeartRate is < 0 or > 400 || request.RespiratoryRate is < 0 or > 250)
            return Result.Failure(SaveConsultationNotesError.InvalidRequest, "Check the heart and respiratory rates.");
        if (request.WeightKg is <= 0 or > 150)
            return Result.Failure(SaveConsultationNotesError.InvalidRequest, "Weight should be in kg, between 0 and 150.");

        var consultation = await _dbContext.Consultations.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (consultation is null)
            return Result.Failure(SaveConsultationNotesError.NotFound, "That consultation no longer exists.");
        if (consultation.IsLocked)
            return Result.Failure(SaveConsultationNotesError.Locked, "This consultation is completed and locked. Add a clinical note instead.");
        if (consultation.Version != request.Version)
            return Result.Failure(SaveConsultationNotesError.ChangedElsewhere, Saving.ChangedElsewhere);

        consultation.Reason = Text.Clean(request.Reason);
        consultation.Subjective = Text.Clean(request.Subjective);
        consultation.Objective = Text.Clean(request.Objective);
        consultation.Assessment = Text.Clean(request.Assessment);
        consultation.Plan = Text.Clean(request.Plan);
        consultation.TemperatureC = request.TemperatureC;
        consultation.HeartRate = request.HeartRate;
        consultation.RespiratoryRate = request.RespiratoryRate;
        consultation.WeightKg = request.WeightKg;
        consultation.MucousMembranes = Text.Clean(request.MucousMembranes);
        consultation.CapillaryRefill = Text.Clean(request.CapillaryRefill);

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(SaveConsultationNotesError.ChangedElsewhere, Saving.ChangedElsewhere);
        return Result.Success(consultation.Version);
    }
}
