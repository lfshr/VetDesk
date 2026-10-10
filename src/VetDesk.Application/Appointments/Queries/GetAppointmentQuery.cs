using MediatR;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Appointments.Queries;

public enum GetAppointmentError { NotSignedIn, NotFound }

public class GetAppointmentQuery : IRequest<Result<AppointmentDto>>
{
    public int Id { get; init; }
}

public class GetAppointmentQueryHandler : IRequestHandler<GetAppointmentQuery, Result<AppointmentDto>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetAppointmentQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<AppointmentDto>> Handle(GetAppointmentQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetAppointmentError.NotSignedIn, Access.NotSignedIn);

        var found = await AppointmentLoader.LoadAsync(_dbContext, q => q.Where(a => a.Id == request.Id), cancellationToken);
        return found.Count == 0
            ? Result.Failure(GetAppointmentError.NotFound, "That appointment no longer exists.")
            : Result.Success(found[0]);
    }
}
