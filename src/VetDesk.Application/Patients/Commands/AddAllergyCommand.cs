using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Patients.Commands;

public enum AddAllergyError { NotAllowed, InvalidRequest, NotFound, AlreadyRecorded }

/// <summary>Records a drug allergy. Only clinical staff can, because prescribing and invoicing are checked against it.</summary>
public class AddAllergyCommand : IRequest<Result<int>>
{
    public int PatientId { get; init; }
    public string Substance { get; init; } = string.Empty;
    public string? DrugClass { get; init; }
}

public class AddAllergyCommandHandler : IRequestHandler<AddAllergyCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public AddAllergyCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<int>> Handle(AddAllergyCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(AddAllergyError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));
        if (Text.Clean(request.Substance) is not { } substance)
            return Result.Failure(AddAllergyError.InvalidRequest, "Enter what the patient is allergic to.");

        var patient = await _dbContext.Patients.Include(p => p.Allergies).FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure(AddAllergyError.NotFound, "That patient no longer exists.");
        if (patient.Allergies.Any(a => a.Substance.Equals(substance, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(AddAllergyError.AlreadyRecorded, $"An allergy to {substance} is already recorded.");

        var allergy = new PatientAllergy { Patient = patient, Substance = substance.ToLowerInvariant(), DrugClass = Text.Clean(request.DrugClass) };
        patient.Allergies.Add(allergy);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(allergy.Id);
    }
}
