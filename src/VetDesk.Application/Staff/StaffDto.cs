using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.Staff;

public record StaffDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string ShortName,
    string Initials,
    string Email,
    StaffRole Role,
    bool IsAdministrator,
    string RoleLabel,
    string JobTitle,
    string? RegistrationNumber,
    bool IsActive,
    DateTime? LastSignInUtc)
{
    public static StaffDto From(StaffMember s) => new(
        s.Id, s.FirstName, s.LastName, s.FullName, s.ShortName, s.Initials, s.Email, s.Role, s.IsAdministrator,
        s.RoleLabel, s.JobTitle, s.RegistrationNumber, s.IsActive,
        s.LastSignInUtc is { } t ? DateTime.SpecifyKind(t, DateTimeKind.Utc) : null);
}

/// <summary>Who signed in and what they may do. Held by the desk for the length of the session.</summary>
public record SignedInStaff(
    int StaffId,
    string FullName,
    string ShortName,
    string Initials,
    string JobTitle,
    StaffRole Role,
    bool IsAdministrator,
    IReadOnlySet<Permission> Permissions);
