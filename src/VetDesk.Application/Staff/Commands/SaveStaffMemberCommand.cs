using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Staff.Commands;

public enum SaveStaffMemberError { NotAllowed, InvalidRequest, NotFound, DuplicateEmail, LastAdministrator }

/// <summary>
/// Adds a member of staff (with a temporary password the administrator hands over) or edits one.
/// The design's "Invite user" sends an email; v1 has no email service, so the administrator sets the first password.
/// </summary>
public class SaveStaffMemberCommand : IRequest<Result<int>>
{
    public int? Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public StaffRole Role { get; init; }
    public bool IsAdministrator { get; init; }
    public string? RegistrationNumber { get; init; }
    public bool IsActive { get; init; } = true;

    /// <summary>Required for a new person; when editing, a value resets their password.</summary>
    public string? TemporaryPassword { get; init; }
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

        if (!Text.Has(request.FirstName) || !Text.Has(request.LastName))
            return Result.Failure(SaveStaffMemberError.InvalidRequest, "Enter a first name and surname.");
        if (!StaffRules.LooksLikeEmail(request.Email.Trim()))
            return Result.Failure(SaveStaffMemberError.InvalidRequest, "Enter a valid email address.");
        if ((request.Id is null || Text.Has(request.TemporaryPassword)) && StaffRules.PasswordProblem(request.TemporaryPassword) is { } problem)
            return Result.Failure(SaveStaffMemberError.InvalidRequest, problem);

        var email = StaffRules.NormaliseEmail(request.Email);
        if (await _dbContext.StaffMembers.AnyAsync(s => s.Email == email && s.Id != request.Id, cancellationToken))
            return Result.Failure(SaveStaffMemberError.DuplicateEmail, $"{email} is already used by another account.");

        StaffMember staff;
        if (request.Id is { } id)
        {
            var existing = await _dbContext.StaffMembers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (existing is null)
                return Result.Failure(SaveStaffMemberError.NotFound, "That member of staff no longer exists.");
            staff = existing;

            var losesAdmin = (staff.IsAdministrator || staff.Role == StaffRole.Admin) && staff.IsActive &&
                             (!request.IsActive || (!request.IsAdministrator && request.Role != StaffRole.Admin));
            if (losesAdmin && !await _dbContext.StaffMembers.AnyAsync(
                    s => s.Id != id && s.IsActive && (s.IsAdministrator || s.Role == StaffRole.Admin), cancellationToken))
                return Result.Failure(SaveStaffMemberError.LastAdministrator,
                    "This is the only administrator. Make someone else an administrator first.");

            staff.FirstName = request.FirstName.Trim();
            staff.LastName = request.LastName.Trim();
            staff.Email = email;
        }
        else
        {
            staff = new StaffMember { FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = email };
            _dbContext.StaffMembers.Add(staff);
        }

        staff.Role = request.Role;
        staff.IsAdministrator = request.IsAdministrator;
        staff.RegistrationNumber = Text.Clean(request.RegistrationNumber);
        staff.IsActive = request.IsActive;
        if (Text.Has(request.TemporaryPassword))
            staff.PasswordHash = _hasher.Hash(request.TemporaryPassword!);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(staff.Id);
    }
}
