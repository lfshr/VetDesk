using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Staff;

public class StaffMember : BaseAuditableEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    /// <summary>Sign-in name. Stored lower case so look-ups don't depend on how it was typed.</summary>
    public required string Email { get; set; }

    public StaffRole Role { get; set; }

    /// <summary>Also has the Admin role's permissions, e.g. a practice owner who is a vet.</summary>
    public bool IsAdministrator { get; set; }

    /// <summary>RCVS registration number for vets and nurses.</summary>
    public string? RegistrationNumber { get; set; }

    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastSignInUtc { get; set; }

    /// <summary>Notifications raised before this time count as read for this person.</summary>
    public DateTime? NotificationsReadUtc { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>How the design refers to staff in lists: "Dr Patel", "Nurse Reid", or the full name.</summary>
    public string ShortName => Role switch
    {
        StaffRole.Vet => $"Dr {LastName}",
        StaffRole.Nurse => $"Nurse {LastName}",
        _ => FullName
    };

    public string DisplayName => Role == StaffRole.Vet ? $"Dr {FullName}" : FullName;

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
