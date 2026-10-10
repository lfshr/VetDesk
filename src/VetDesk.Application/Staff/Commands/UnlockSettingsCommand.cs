using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Staff.Commands;

public enum UnlockSettingsError { NotSignedIn, NotAdministrator, WrongPassword }

/// <summary>
/// An administrator signed in by PIN confirms their password to open users and settings. Lasts until the desk
/// locks or they sign out, so a PIN seen over a shoulder never reaches the settings.
/// </summary>
public class UnlockSettingsCommand : IRequest<Result<SignedInStaff>>
{
    public string Password { get; init; } = string.Empty;
}

public class UnlockSettingsCommandHandler : IRequestHandler<UnlockSettingsCommand, Result<SignedInStaff>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IPasswordHasher _hasher;

    public UnlockSettingsCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IPasswordHasher hasher)
    {
        _dbContext = dbContext;
        _user = user;
        _hasher = hasher;
    }

    public async Task<Result<SignedInStaff>> Handle(UnlockSettingsCommand request, CancellationToken cancellationToken)
    {
        if (_user.StaffId is not { } staffId)
            return Result.Failure(UnlockSettingsError.NotSignedIn, Access.NotSignedIn);

        var staff = await _dbContext.StaffMembers.FirstAsync(s => s.Id == staffId, cancellationToken);
        var signedIn = await StaffRules.SignedInAsync(_dbContext, staff, passwordConfirmed: true, cancellationToken);
        if (!signedIn.Permissions.Contains(Permission.ManageSettings))
            return Result.Failure(UnlockSettingsError.NotAdministrator, "Only practice administrators can open users and settings.");
        if (!staff.HasPassword || string.IsNullOrEmpty(request.Password) || !_hasher.Verify(staff.PasswordHash, request.Password))
            return Result.Failure(UnlockSettingsError.WrongPassword, "That password isn't right.");

        return Result.Success(signedIn);
    }
}
