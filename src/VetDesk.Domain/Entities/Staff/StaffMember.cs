using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Staff;

public class StaffMember : BaseAuditableEntity
{
    /// <summary>Wrong PINs in a row before sign-in by PIN stops for <see cref="PinLockout"/>.</summary>
    public const int MaxPinAttempts = 5;

    public static readonly TimeSpan PinLockout = TimeSpan.FromMinutes(5);

    public required string FirstName { get; set; }

    /// <summary>Optional: a small practice often knows everyone by first name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Contact only; sign-in is by name and PIN. Stored lower case.</summary>
    public string? Email { get; set; }

    public StaffRole Role { get; set; }

    /// <summary>Also has the Admin role's permissions, e.g. a practice owner who is a vet.</summary>
    public bool IsAdministrator { get; set; }

    /// <summary>RCVS registration number for vets and nurses.</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>The 4-digit PIN everyone signs in, unlocks and witnesses with.</summary>
    public string PinHash { get; set; } = string.Empty;

    public int FailedPinAttempts { get; set; }
    public DateTime? PinLockedUntilUtc { get; set; }

    /// <summary>Administrators only: confirms it's really them before users and settings open.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime? LastSignInUtc { get; set; }

    /// <summary>Notifications raised before this time count as read for this person.</summary>
    public DateTime? NotificationsReadUtc { get; set; }

    public bool HasPin => PinHash.Length > 0;
    public bool HasPassword => PasswordHash.Length > 0;
    public bool IsAnyAdministrator => IsAdministrator || Role == StaffRole.Admin;

    /// <summary>
    /// Vets and administrators book closed days and busy times themselves; anyone else needs one of them to approve.
    /// </summary>
    public static bool ApprovesBookings(StaffRole role, bool isAdministrator) =>
        role is StaffRole.Vet or StaffRole.Admin || isAdministrator;

    public bool CanApproveBookings => ApprovesBookings(Role, IsAdministrator);

    public bool IsPinLocked(DateTime nowUtc) => PinLockedUntilUtc > nowUtc;

    /// <summary>Counts a wrong PIN; the last allowed attempt locks PIN sign-in for a few minutes.</summary>
    public void RecordWrongPin(DateTime nowUtc)
    {
        FailedPinAttempts++;
        if (FailedPinAttempts < MaxPinAttempts)
            return;
        PinLockedUntilUtc = nowUtc + PinLockout;
        FailedPinAttempts = 0;
    }

    public void RecordRightPin()
    {
        FailedPinAttempts = 0;
        PinLockedUntilUtc = null;
    }

    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>How staff are written into lists and records: their first name, no "Dr" or "Nurse".</summary>
    public string ShortName => FirstName;

    public string Initials =>
        string.Concat(new[] { FirstName, LastName }.Where(n => n.Length > 0).Select(n => char.ToUpperInvariant(n[0])));

    public string RoleLabel => IsAdministrator && Role != StaffRole.Admin ? $"{Role} (admin)" : Role.ToString();

    public string JobTitle => Role switch
    {
        StaffRole.Vet => "Veterinary surgeon",
        StaffRole.Nurse => "Veterinary nurse",
        StaffRole.Reception => "Reception",
        _ => "Practice administrator"
    };
}

public enum StaffRole
{
    Vet,
    Nurse,
    Reception,
    Admin
}
