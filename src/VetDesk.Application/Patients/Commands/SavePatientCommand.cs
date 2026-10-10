using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Patients.Commands;

public enum SavePatientError { NotAllowed, InvalidRequest, NotFound, ClientNotFound, ChangedElsewhere }

/// <summary>Adds a pet to an existing client (Id null) or edits a patient's details.</summary>
public class SavePatientCommand : IRequest<Result<int>>
{
    public int? Id { get; init; }
    public int ClientId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Species { get; init; } = "Dog";
    public string? Breed { get; init; }
    public Sex Sex { get; init; }
    public bool IsNeutered { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Microchip { get; init; }
    public bool IsDeceased { get; init; }
    public Guid? Version { get; init; }
}

public class SavePatientCommandHandler : IRequestHandler<SavePatientCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public SavePatientCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<int>> Handle(SavePatientCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(SavePatientError.NotAllowed, Access.Denied(Permission.ViewClinicalRecords));
        if (PatientRules.Validate(request.Name, request.Species, request.DateOfBirth, ClinicTime.Today(_clock.UtcNow)) is { } problem)
            return Result.Failure(SavePatientError.InvalidRequest, problem);

        Patient patient;
        if (request.Id is { } id)
        {
            var existing = await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (existing is null)
                return Result.Failure(SavePatientError.NotFound, "That patient no longer exists.");
            if (request.Version is { } version && existing.Version != version)
                return Result.Failure(SavePatientError.ChangedElsewhere, Saving.ChangedElsewhere);
            patient = existing;
            patient.Name = request.Name.Trim();
            patient.Species = PatientRules.CleanSpecies(request.Species);
        }
        else
        {
            var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);
            if (client is null)
                return Result.Failure(SavePatientError.ClientNotFound, "That client no longer exists.");
            patient = new Patient { Name = request.Name.Trim(), Client = client, Species = PatientRules.CleanSpecies(request.Species) };
            _dbContext.Patients.Add(patient);
        }

        patient.Breed = Text.Clean(request.Breed);
        patient.Sex = request.Sex;
        patient.IsNeutered = request.IsNeutered;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Microchip = PatientRules.CleanMicrochip(request.Microchip);
        patient.IsDeceased = request.IsDeceased;

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(SavePatientError.ChangedElsewhere, Saving.ChangedElsewhere);
        return Result.Success(patient.Id);
    }
}
