using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Staff.Commands;

public enum SetUpPinError { InvalidRequest, NotAllowed, WrongPassword }

/// <summary>
/// For an account from before PINs (a password but no PIN): the person proves who they are with the password once,
/// chooses a PIN and is signed in. Anyone without either asks an administrator to set a PIN instead.
/// </summary>
public class SetUpPinCommand : IRequest<Result<SignedInStaff>>
{
    public int StaffId { get; init; }
    public string Password { get; init; } = string.Empty;
    public string NewPin { get; init; } = string.Empty;
}

public class SetUpPinCommandHandler : IRequestHandler<SetUpPinCommand, Result<SignedInStaff>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;

    public SetUpPinCommandHandler(IVetDeskDbContext dbContext, IPasswordHasher hasher, IClock clock)
    {
        _dbContext = dbContext;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<Result<SignedInStaff>> Handle(SetUpPinCommand request, CancellationToken cancellationToken)
    {
        if (StaffRules.PinProblem(request.NewPin) is { } problem)
            return Result.Failure(SetUpPinError.InvalidRequest, problem);

        var staff = await _dbContext.StaffMembers.FirstOrDefaultAsync(s => s.Id == request.StaffId && s.IsActive, cancellationToken);
        if (staff is null || staff.HasPin || !staff.HasPassword)
            return Result.Failure(SetUpPinError.NotAllowed, "Ask a practice administrator to set your PIN in Settings › Users and roles.");
        if (string.IsNullOrEmpty(request.Password) || !_hasher.Verify(staff.PasswordHash, request.Password))
            return Result.Failure(SetUpPinError.WrongPassword, "That password isn't right.");

        staff.PinHash = _hasher.Hash(request.NewPin);
        staff.LastSignInUtc = _clock.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await StaffRules.SignedInAsync(_dbContext, staff, passwordConfirmed: true, cancellationToken));
    }
}
