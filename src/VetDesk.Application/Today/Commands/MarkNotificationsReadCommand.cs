using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Today.Commands;

public enum MarkNotificationsReadError { NotSignedIn }

/// <summary>"Mark all read" for the signed-in person only; colleagues keep their own unread count.</summary>
public class MarkNotificationsReadCommand : IRequest<Result>
{
}

public class MarkNotificationsReadCommandHandler : IRequestHandler<MarkNotificationsReadCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public MarkNotificationsReadCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result> Handle(MarkNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        if (_user.StaffId is not { } staffId)
            return Result.Failure(MarkNotificationsReadError.NotSignedIn, Access.NotSignedIn);

        var staff = await _dbContext.StaffMembers.FirstAsync(s => s.Id == staffId, cancellationToken);
        staff.NotificationsReadUtc = _clock.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
