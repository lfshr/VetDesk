using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Staff.Commands;

public enum CreateFirstAdministratorError { InvalidRequest, AlreadySetUp }

/// <summary>
/// Creates the first account on a new database. Only works while there are no staff at all, so it can't be used to
/// add an account once the practice is set up. Signs them in with settings already open, since they just set the
/// password.
/// </summary>
public class CreateFirstAdministratorCommand : IRequest<Result<SignedInStaff>>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string Pin { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public StaffRole Role { get; init; } = StaffRole.Admin;
}

public class CreateFirstAdministratorCommandHandler : IRequestHandler<CreateFirstAdministratorCommand, Result<SignedInStaff>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;

    public CreateFirstAdministratorCommandHandler(IVetDeskDbContext dbContext, IPasswordHasher hasher, IClock clock)
    {
        _dbContext = dbContext;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<Result<SignedInStaff>> Handle(CreateFirstAdministratorCommand request, CancellationToken cancellationToken)
    {
        if (!Text.Has(request.FirstName))
            return Result.Failure(CreateFirstAdministratorError.InvalidRequest, "Enter a first name.");
        if (Text.Has(request.Email) && !StaffRules.LooksLikeEmail(request.Email!.Trim()))
            return Result.Failure(CreateFirstAdministratorError.InvalidRequest, "Enter a valid email address, or leave it blank.");
        if (StaffRules.PinProblem(request.Pin) is { } pinProblem)
            return Result.Failure(CreateFirstAdministratorError.InvalidRequest, pinProblem);
        if (StaffRules.PasswordProblem(request.Password) is { } problem)
            return Result.Failure(CreateFirstAdministratorError.InvalidRequest, problem);

        if (await _dbContext.StaffMembers.AnyAsync(cancellationToken))
            return Result.Failure(CreateFirstAdministratorError.AlreadySetUp, "VetDesk is already set up. Sign in instead.");

        var staff = new StaffMember
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = Text.Has(request.Email) ? StaffRules.NormaliseEmail(request.Email!) : null,
            Role = request.Role,
            IsAdministrator = true,
            PinHash = _hasher.Hash(request.Pin),
            PasswordHash = _hasher.Hash(request.Password),
            LastSignInUtc = _clock.UtcNow,
        };
        _dbContext.StaffMembers.Add(staff);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await StaffRules.SignedInAsync(_dbContext, staff, passwordConfirmed: true, cancellationToken));
    }
}
