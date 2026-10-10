using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.Staff;

public record StaffDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string ShortName,
    string Initials,
    string? Email,
    StaffRole Role,
    bool IsAdministrator,
    string RoleLabel,
    string JobTitle,
    string? RegistrationNumber,
    bool IsActive,
    bool HasPin,
    bool HasPassword,
    DateTime? LastSignInUtc)
{
    public static StaffDto From(StaffMember s) => new(
        s.Id, s.FirstName, s.LastName, s.FullName, s.ShortName, s.Initials, s.Email, s.Role, s.IsAdministrator,
        s.RoleLabel, s.JobTitle, s.RegistrationNumber, s.IsActive, s.HasPin, s.HasPassword,
        s.LastSignInUtc is { } t ? DateTime.SpecifyKind(t, DateTimeKind.Utc) : null);
}

/// <summary>A tile on the sign-in screen. Shown before anyone signs in, so only what the tile needs.</summary>
/// <param name="CanSetUpPin">No PIN yet but a password from before PINs, so they can choose their own PIN.</param>
public record SignInTileDto(int Id, string Name, string Initials, string JobTitle, bool HasPin, bool CanSetUpPin);

/// <summary>Who signed in and what they may do. Held by the desk for the length of the session.</summary>
/// <param name="CanUnlockSettings">An administrator signed in by PIN: their password opens users and settings.</param>
public record SignedInStaff(
    int StaffId,
    string FullName,
    string ShortName,
    string Initials,
    string JobTitle,
    StaffRole Role,
    bool IsAdministrator,
    IReadOnlySet<Permission> Permissions,
    bool CanUnlockSettings = false);
