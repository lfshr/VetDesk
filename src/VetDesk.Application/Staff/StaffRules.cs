using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.Staff;

internal static class StaffRules
{
    public const int MinPasswordLength = 10;
    public const int PinLength = 4;

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

    public static string? PinProblem(string? pin) =>
        pin is null || pin.Length != PinLength || !pin.All(char.IsAsciiDigit)
            ? $"Use a PIN of {PinLength} digits."
            : null;

    /// <summary>
    /// Checks a PIN and keeps count of wrong ones, saving the count straight away so a failed attempt still counts
    /// when the caller returns without saving. Call it before the request changes anything else.
    /// </summary>
    /// <returns>Why it was refused, or null when the PIN is right.</returns>
    public static async Task<string?> CheckPinAsync(
        IVetDeskDbContext db, IPasswordHasher hasher, StaffMember staff, string? pin, DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (!staff.HasPin)
            return $"{staff.FirstName} has no PIN yet. A practice administrator can set one in Settings › Users and roles.";
        if (staff.IsPinLocked(nowUtc))
            return $"Too many wrong PINs for {staff.FirstName}. Try again in a few minutes.";

        if (!string.IsNullOrEmpty(pin) && hasher.Verify(staff.PinHash, pin))
        {
            if (staff.FailedPinAttempts > 0 || staff.PinLockedUntilUtc is not null)
            {
                staff.RecordRightPin();
                await db.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        staff.RecordWrongPin(nowUtc);
        await db.SaveChangesAsync(cancellationToken);
        return staff.IsPinLocked(nowUtc)
            ? $"Too many wrong PINs for {staff.FirstName}. Try again in a few minutes."
            : $"That isn't {staff.FirstName}'s PIN.";
    }

    /// <summary>
    /// A member of staff's permissions: their role's, plus the Admin role's when they are an administrator.
    /// Managing users and settings also needs the password, so a PIN alone never grants it.
    /// </summary>
    public static async Task<IReadOnlySet<Permission>> PermissionsForAsync(
        IVetDeskDbContext db, StaffMember staff, CancellationToken cancellationToken, bool passwordConfirmed = true)
    {
        var roles = staff.IsAdministrator ? new[] { staff.Role, StaffRole.Admin } : new[] { staff.Role };
        var permissions = await db.RolePermissions
            .Where(rp => roles.Contains(rp.Role))
            .Select(rp => rp.Permission)
            .ToListAsync(cancellationToken);
        var set = permissions.ToHashSet();
        if (!passwordConfirmed)
            set.Remove(Permission.ManageSettings);
        return set;
    }

    public static async Task<SignedInStaff> SignedInAsync(
        IVetDeskDbContext db, StaffMember staff, bool passwordConfirmed, CancellationToken cancellationToken)
    {
        var all = await PermissionsForAsync(db, staff, cancellationToken);
        var granted = passwordConfirmed ? all : await PermissionsForAsync(db, staff, cancellationToken, passwordConfirmed: false);
        return new(staff.Id, staff.FullName, staff.ShortName, staff.Initials, staff.JobTitle, staff.Role, staff.IsAdministrator,
            granted, CanUnlockSettings: !granted.Contains(Permission.ManageSettings) && all.Contains(Permission.ManageSettings));
    }
}
