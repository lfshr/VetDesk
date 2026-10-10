using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Staff.Commands;

public enum SaveStaffMemberError { NotAllowed, InvalidRequest, NotFound, DuplicateEmail, LastAdministrator }

/// <summary>
/// Adds a member of staff or edits one. The administrator sets the first PIN and hands it over; administrators also
/// need a password, which opens users and settings.
/// </summary>
public class SaveStaffMemberCommand : IRequest<Result<int>>
{
    public int? Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public StaffRole Role { get; init; }
    public bool IsAdministrator { get; init; }
    public string? RegistrationNumber { get; init; }
    public bool IsActive { get; init; } = true;

    /// <summary>Required for a new person; when editing, a value resets their PIN and clears a PIN lock-out.</summary>
    public string? Pin { get; init; }

    /// <summary>Administrators only. Required when someone becomes an administrator without one; a value resets it.</summary>
    public string? Password { get; init; }
}

public class SaveStaffMemberCommandHandler : IRequestHandler<SaveStaffMemberCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IPasswordHasher _hasher;

    public SaveStaffMemberCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IPasswordHasher hasher)
    {
        _dbContext = dbContext;
        _user = user;
        _hasher = hasher;
    }

    public async Task<Result<int>> Handle(SaveStaffMemberCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ManageSettings))
            return Result.Failure(SaveStaffMemberError.NotAllowed, Access.Denied(Permission.ManageSettings));

        if (!Text.Has(request.FirstName))
            return Result.Failure(SaveStaffMemberError.InvalidRequest, "Enter a first name.");
        if (Text.Has(request.Email) && !StaffRules.LooksLikeEmail(request.Email!.Trim()))
            return Result.Failure(SaveStaffMemberError.InvalidRequest, "Enter a valid email address, or leave it blank.");
        if ((request.Id is null || Text.Has(request.Pin)) && StaffRules.PinProblem(request.Pin) is { } pinProblem)
            return Result.Failure(SaveStaffMemberError.InvalidRequest, pinProblem);
        if (Text.Has(request.Password) && StaffRules.PasswordProblem(request.Password) is { } passwordProblem)
            return Result.Failure(SaveStaffMemberError.InvalidRequest, passwordProblem);

        var email = Text.Has(request.Email) ? StaffRules.NormaliseEmail(request.Email!) : null;
        if (email is not null && await _dbContext.StaffMembers.AnyAsync(s => s.Email == email && s.Id != request.Id, cancellationToken))
            return Result.Failure(SaveStaffMemberError.DuplicateEmail, $"{email} is already used by another account.");

        StaffMember staff;
        if (request.Id is { } id)
        {
            var existing = await _dbContext.StaffMembers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (existing is null)
                return Result.Failure(SaveStaffMemberError.NotFound, "That member of staff no longer exists.");
            staff = existing;

            var losesAdmin = staff.IsAnyAdministrator && staff.IsActive &&
                             (!request.IsActive || (!request.IsAdministrator && request.Role != StaffRole.Admin));
            if (losesAdmin && !await _dbContext.StaffMembers.AnyAsync(
                    s => s.Id != id && s.IsActive && (s.IsAdministrator || s.Role == StaffRole.Admin), cancellationToken))
                return Result.Failure(SaveStaffMemberError.LastAdministrator,
                    "This is the only administrator. Make someone else an administrator first.");

            staff.FirstName = request.FirstName.Trim();
        }
        else
        {
            staff = new StaffMember { FirstName = request.FirstName.Trim() };
            _dbContext.StaffMembers.Add(staff);
        }

        var becomesAdmin = request.IsAdministrator || request.Role == StaffRole.Admin;
        if (becomesAdmin && !staff.HasPassword && !Text.Has(request.Password))
            return Result.Failure(SaveStaffMemberError.InvalidRequest,
                $"Administrators need a password of at least {StaffRules.MinPasswordLength} characters to open users and settings.");

        staff.LastName = request.LastName.Trim();
        staff.Email = email;
        staff.Role = request.Role;
        staff.IsAdministrator = request.IsAdministrator;
        staff.RegistrationNumber = Text.Clean(request.RegistrationNumber);
        staff.IsActive = request.IsActive;
        if (Text.Has(request.Pin))
        {
            staff.PinHash = _hasher.Hash(request.Pin!);
            staff.RecordRightPin();
        }

        if (Text.Has(request.Password))
            staff.PasswordHash = _hasher.Hash(request.Password!);
        else if (!becomesAdmin)
            staff.PasswordHash = string.Empty;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(staff.Id);
    }
}
