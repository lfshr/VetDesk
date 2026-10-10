using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Tasks;
using SimpleResult;

namespace VetDesk.Application.Today.Commands;

public enum AddFollowUpError { NotSignedIn, InvalidRequest, PatientNotFound }

public class AddFollowUpCommand : IRequest<Result<int>>
{
    public string Text { get; init; } = string.Empty;
    public int? PatientId { get; init; }
    public DateOnly? DueDate { get; init; }
}

public class AddFollowUpCommandHandler : IRequestHandler<AddFollowUpCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public AddFollowUpCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<int>> Handle(AddFollowUpCommand request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(AddFollowUpError.NotSignedIn, Access.NotSignedIn);
        if (Text.Clean(request.Text) is not { } text)
            return Result.Failure(AddFollowUpError.InvalidRequest, "Say what needs following up.");

        var patient = request.PatientId is { } patientId
            ? await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken)
            : null;
        if (request.PatientId is not null && patient is null)
            return Result.Failure(AddFollowUpError.PatientNotFound, "That patient no longer exists.");

        var followUp = new FollowUp { Text = text, Patient = patient, DueDate = request.DueDate ?? ClinicTime.Today(_clock.UtcNow) };
        _dbContext.FollowUps.Add(followUp);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(followUp.Id);
    }
}
