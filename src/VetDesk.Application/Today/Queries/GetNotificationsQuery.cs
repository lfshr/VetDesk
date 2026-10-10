using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Today.Queries;

public enum GetNotificationsError { NotSignedIn }

public class GetNotificationsQuery : IRequest<Result<NotificationFeed>>
{
    public int Take { get; init; } = 20;
}

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, Result<NotificationFeed>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetNotificationsQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<NotificationFeed>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (_user.StaffId is not { } staffId)
            return Result.Failure(GetNotificationsError.NotSignedIn, Access.NotSignedIn);

        var readUpTo = await _dbContext.StaffMembers.Where(s => s.Id == staffId).Select(s => s.NotificationsReadUtc).FirstOrDefaultAsync(cancellationToken)
                       ?? DateTime.MinValue;

        var items = await _dbContext.Notifications.AsNoTracking()
            .OrderByDescending(n => n.CreatedUtc).ThenByDescending(n => n.Id)
            .Take(request.Take)
            .ToListAsync(cancellationToken);
        var unread = await _dbContext.Notifications.CountAsync(n => n.CreatedUtc > readUpTo, cancellationToken);

        return Result.Success(new NotificationFeed(
            items.Select(n => new NotificationDto(n.Id, Utc.Mark(n.CreatedUtc), n.Text, n.Kind, n.Link, n.CreatedUtc > readUpTo)).ToList(),
            unread));
    }
}
