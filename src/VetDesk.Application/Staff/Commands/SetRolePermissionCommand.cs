using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Staff.Commands;

public enum SetRolePermissionError { NotAllowed, WouldLockOut }

/// <summary>Ticks or clears one box in the role permissions table. Takes effect at each person's next sign-in.</summary>
public class SetRolePermissionCommand : IRequest<Result>
{
    public StaffRole Role { get; init; }
    public Permission Permission { get; init; }
    public bool Granted { get; init; }
}

public class SetRolePermissionCommandHandler : IRequestHandler<SetRolePermissionCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public SetRolePermissionCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result> Handle(SetRolePermissionCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ManageSettings))
            return Result.Failure(SetRolePermissionError.NotAllowed, Access.Denied(Permission.ManageSettings));

        // Without this nobody could ever open Settings again.
        if (request is { Role: StaffRole.Admin, Permission: Permission.ManageSettings, Granted: false })
            return Result.Failure(SetRolePermissionError.WouldLockOut, "Administrators must keep access to users and settings.");

        var existing = await _dbContext.RolePermissions
            .FirstOrDefaultAsync(rp => rp.Role == request.Role && rp.Permission == request.Permission, cancellationToken);

        if (request.Granted && existing is null)
            _dbContext.RolePermissions.Add(new RolePermission { Role = request.Role, Permission = request.Permission });
        else if (!request.Granted && existing is not null)
            _dbContext.RolePermissions.Remove(existing);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
