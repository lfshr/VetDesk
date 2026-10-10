using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Patients.Commands;

public enum RemoveAllergyError { NotAllowed }

public class RemoveAllergyCommand : IRequest<Result>
{
    public int AllergyId { get; init; }
}

public class RemoveAllergyCommandHandler : IRequestHandler<RemoveAllergyCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public RemoveAllergyCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result> Handle(RemoveAllergyCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(RemoveAllergyError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));

        var allergy = await _dbContext.PatientAllergies.FirstOrDefaultAsync(a => a.Id == request.AllergyId, cancellationToken);
        if (allergy is not null)
        {
            _dbContext.PatientAllergies.Remove(allergy);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
