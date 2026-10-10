using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Consultations.Commands;

public enum RemoveChargeError { NotAllowed, Locked }

public class RemoveChargeCommand : IRequest<Result>
{
    public int ChargeId { get; init; }
}

public class RemoveChargeCommandHandler : IRequestHandler<RemoveChargeCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public RemoveChargeCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result> Handle(RemoveChargeCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(RemoveChargeError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));

        var charge = await _dbContext.ConsultationCharges.Include(c => c.Consultation)
            .FirstOrDefaultAsync(c => c.Id == request.ChargeId, cancellationToken);
        if (charge is null)
            return Result.Success();
        if (charge.Consultation.IsLocked)
            return Result.Failure(RemoveChargeError.Locked, "This consultation is completed and locked.");

        _dbContext.ConsultationCharges.Remove(charge);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
