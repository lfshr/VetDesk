using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Patients.Commands;

public enum DeletePatientAlertError { NotAllowed }

public class DeletePatientAlertCommand : IRequest<Result>
{
    public int AlertId { get; init; }
}

public class DeletePatientAlertCommandHandler : IRequestHandler<DeletePatientAlertCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public DeletePatientAlertCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result> Handle(DeletePatientAlertCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(DeletePatientAlertError.NotAllowed, Access.Denied(Permission.ViewClinicalRecords));

        // Already gone (removed at another desk) is the outcome the user wanted, so it isn't an error.
        var alert = await _dbContext.PatientAlerts.FirstOrDefaultAsync(a => a.Id == request.AlertId, cancellationToken);
        if (alert is not null)
        {
            _dbContext.PatientAlerts.Remove(alert);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
