using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Today.Commands;

public enum SetFollowUpDoneError { NotSignedIn, NotFound }

public class SetFollowUpDoneCommand : IRequest<Result>
{
    public int Id { get; init; }
    public bool IsDone { get; init; }
}

public class SetFollowUpDoneCommandHandler : IRequestHandler<SetFollowUpDoneCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public SetFollowUpDoneCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result> Handle(SetFollowUpDoneCommand request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(SetFollowUpDoneError.NotSignedIn, Access.NotSignedIn);

        var followUp = await _dbContext.FollowUps.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
        if (followUp is null)
            return Result.Failure(SetFollowUpDoneError.NotFound, "That follow-up no longer exists.");

        followUp.IsDone = request.IsDone;
        followUp.DoneUtc = request.IsDone ? _clock.UtcNow : null;
        followUp.DoneBy = request.IsDone ? _user.Name : null;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
