using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.Staff;

internal static class StaffRules
{
    public const int MinPasswordLength = 10;

    public static string NormaliseEmail(string email) => email.Trim().ToLowerInvariant();

    public static bool LooksLikeEmail(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 && at < email.Length - 1 && !email.Contains(' ');
    }

    public static string? PasswordProblem(string? password) =>
        string.IsNullOrEmpty(password) || password.Length < MinPasswordLength
            ? $"Use a password of at least {MinPasswordLength} characters."
            : null;

    /// <summary>A member of staff's permissions: their role's, plus the Admin role's when they are an administrator.</summary>
    public static async Task<IReadOnlySet<Permission>> PermissionsForAsync(IVetDeskDbContext db, StaffMember staff, CancellationToken cancellationToken)
    {
        var roles = staff.IsAdministrator ? new[] { staff.Role, StaffRole.Admin } : new[] { staff.Role };
        var permissions = await db.RolePermissions
            .Where(rp => roles.Contains(rp.Role))
            .Select(rp => rp.Permission)
            .ToListAsync(cancellationToken);
        return permissions.ToHashSet();
    }

    public static async Task<SignedInStaff> SignedInAsync(IVetDeskDbContext db, StaffMember staff, CancellationToken cancellationToken) =>
        new(staff.Id, staff.FullName, staff.ShortName, staff.Initials, staff.JobTitle, staff.Role, staff.IsAdministrator,
            await PermissionsForAsync(db, staff, cancellationToken));
}
