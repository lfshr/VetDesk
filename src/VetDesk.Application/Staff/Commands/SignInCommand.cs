using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Staff.Commands;

public enum SignInError { InvalidCredentials }

public class SignInCommand : IRequest<Result<SignedInStaff>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class SignInCommandHandler : IRequestHandler<SignInCommand, Result<SignedInStaff>>
{
    // One message for every failure, so the screen doesn't reveal which emails have accounts.
    internal const string Rejected = "That email and password don't match an active account.";

    private readonly IVetDeskDbContext _dbContext;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;

    public SignInCommandHandler(IVetDeskDbContext dbContext, IPasswordHasher hasher, IClock clock)
    {
        _dbContext = dbContext;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<Result<SignedInStaff>> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        var email = StaffRules.NormaliseEmail(request.Email);
        var staff = await _dbContext.StaffMembers.FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

        if (staff is null || !staff.IsActive || string.IsNullOrEmpty(request.Password) ||
            !_hasher.Verify(staff.PasswordHash, request.Password))
            return Result.Failure(SignInError.InvalidCredentials, Rejected);

        staff.LastSignInUtc = _clock.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await StaffRules.SignedInAsync(_dbContext, staff, cancellationToken));
    }
}
