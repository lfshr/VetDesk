using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Staff.Commands;

public enum ChangePasswordError { NotSignedIn, InvalidRequest, WrongPassword }

public class ChangePasswordCommand : IRequest<Result>
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IPasswordHasher _hasher;

    public ChangePasswordCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IPasswordHasher hasher)
    {
        _dbContext = dbContext;
        _user = user;
        _hasher = hasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_user.StaffId is not { } staffId)
            return Result.Failure(ChangePasswordError.NotSignedIn, Access.NotSignedIn);
        if (StaffRules.PasswordProblem(request.NewPassword) is { } problem)
            return Result.Failure(ChangePasswordError.InvalidRequest, problem);

        var staff = await _dbContext.StaffMembers.FirstAsync(s => s.Id == staffId, cancellationToken);
        if (!_hasher.Verify(staff.PasswordHash, request.CurrentPassword))
            return Result.Failure(ChangePasswordError.WrongPassword, "Your current password isn't right.");

        staff.PasswordHash = _hasher.Hash(request.NewPassword);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
