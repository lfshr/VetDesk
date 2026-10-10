using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Appointments.Queries;

public enum GetAppointmentTypesError { NotSignedIn }

public class GetAppointmentTypesQuery : IRequest<Result<IReadOnlyList<AppointmentTypeDto>>>
{
    public bool IncludeInactive { get; init; }
}

public class GetAppointmentTypesQueryHandler : IRequestHandler<GetAppointmentTypesQuery, Result<IReadOnlyList<AppointmentTypeDto>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetAppointmentTypesQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<IReadOnlyList<AppointmentTypeDto>>> Handle(GetAppointmentTypesQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetAppointmentTypesError.NotSignedIn, Access.NotSignedIn);

        var types = await _dbContext.AppointmentTypes.AsNoTracking()
            .Where(t => request.IncludeInactive || t.IsActive)
            .OrderBy(t => t.Kind).ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
        IReadOnlyList<AppointmentTypeDto> dtos = types.Select(AppointmentTypeDto.From).ToList();
        return Result.Success(dtos);
    }
}
