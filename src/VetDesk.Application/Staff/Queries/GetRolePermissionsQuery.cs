using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Staff.Queries;

public enum GetRolePermissionsError { NotSignedIn }

public class GetRolePermissionsQuery : IRequest<Result<IReadOnlyList<(StaffRole Role, Permission Permission)>>>
{
}

public class GetRolePermissionsQueryHandler
    : IRequestHandler<GetRolePermissionsQuery, Result<IReadOnlyList<(StaffRole Role, Permission Permission)>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetRolePermissionsQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<IReadOnlyList<(StaffRole Role, Permission Permission)>>> Handle(
        GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetRolePermissionsError.NotSignedIn, Access.NotSignedIn);

        var rows = await _dbContext.RolePermissions.AsNoTracking().ToListAsync(cancellationToken);
        IReadOnlyList<(StaffRole, Permission)> grid = rows.Select(r => (r.Role, r.Permission)).ToList();
        return Result.Success(grid);
    }
}
