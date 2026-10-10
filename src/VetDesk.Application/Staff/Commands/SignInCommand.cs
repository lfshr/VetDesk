using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Staff.Commands;

public enum SignInError { InvalidCredentials }

/// <summary>
/// Signs in (or unlocks the desk) by tapping your name and entering your PIN. Users and settings stay closed until an
/// administrator also confirms their password with <see cref="UnlockSettingsCommand"/>.
/// </summary>
public class SignInCommand : IRequest<Result<SignedInStaff>>
{
    public int StaffId { get; init; }
    public string Pin { get; init; } = string.Empty;
}

public class SignInCommandHandler : IRequestHandler<SignInCommand, Result<SignedInStaff>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly IDevicePinLimit _deviceLimit;

    public SignInCommandHandler(IVetDeskDbContext dbContext, IPasswordHasher hasher, IClock clock, IDevicePinLimit deviceLimit)
    {
        _dbContext = dbContext;
        _hasher = hasher;
        _clock = clock;
        _deviceLimit = deviceLimit;
    }

    public async Task<Result<SignedInStaff>> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        var staff = await _dbContext.StaffMembers.FirstOrDefaultAsync(s => s.Id == request.StaffId && s.IsActive, cancellationToken);
        if (staff is null)
            return Result.Failure(SignInError.InvalidCredentials, "That account isn't active. Ask a practice administrator.");

        var now = _clock.UtcNow;
        if (await StaffRules.CheckPinAsync(_dbContext, _hasher, _deviceLimit, staff, request.Pin, now, cancellationToken) is { } problem)
            return Result.Failure(SignInError.InvalidCredentials, problem);

        staff.LastSignInUtc = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await StaffRules.SignedInAsync(_dbContext, staff, passwordConfirmed: false, cancellationToken));
    }
}
