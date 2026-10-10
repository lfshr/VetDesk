using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Staff.Queries;

public enum GetStaffError { NotSignedIn }

public class GetStaffQuery : IRequest<Result<IReadOnlyList<StaffDto>>>
{
    public bool IncludeInactive { get; init; }

    /// <summary>Only people who may witness controlled drugs, for the witness picker.</summary>
    public bool WitnessesOnly { get; init; }

    public StaffRole? Role { get; init; }
}

public class GetStaffQueryHandler : IRequestHandler<GetStaffQuery, Result<IReadOnlyList<StaffDto>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetStaffQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<IReadOnlyList<StaffDto>>> Handle(GetStaffQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetStaffError.NotSignedIn, Access.NotSignedIn);

        var query = _dbContext.StaffMembers.AsNoTracking();
        if (!request.IncludeInactive)
            query = query.Where(s => s.IsActive);
        if (request.Role is { } role)
            query = query.Where(s => s.Role == role);
        if (request.WitnessesOnly)
        {
            var roles = await _dbContext.RolePermissions
                .Where(rp => rp.Permission == Permission.WitnessControlledDrugs)
                .Select(rp => rp.Role)
                .ToListAsync(cancellationToken);
            var adminsCanWitness = roles.Contains(StaffRole.Admin);
            query = query.Where(s => roles.Contains(s.Role) || (adminsCanWitness && s.IsAdministrator));
        }

        var staff = await query.OrderBy(s => s.Role).ThenBy(s => s.LastName).ToListAsync(cancellationToken);
        IReadOnlyList<StaffDto> dtos = staff.Select(StaffDto.From).ToList();
        return Result.Success(dtos);
    }
}
