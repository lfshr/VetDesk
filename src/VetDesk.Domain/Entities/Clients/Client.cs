using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Patients;

namespace VetDesk.Domain.Entities.Clients;

/// <summary>A pet owner. Called a client throughout the design.</summary>
public class Client : BaseAuditableEntity, IHasVersion
{
    public string FirstName { get; set; } = string.Empty;
    public required string LastName { get; set; }
    private string _phone = string.Empty;

    public required string Phone
    {
        get => _phone;
        set
        {
            _phone = value.Trim();
            PhoneDigits = DigitsOf(value);
        }
    }

    /// <summary>The phone number reduced to digits, so "07700 900412" and "+44 7700 900412" are found as the same.</summary>
    public string PhoneDigits { get; private set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Postcode { get; set; }

    /// <summary>Whether the owner agreed to reminders by text and email.</summary>
    public bool RemindersOptIn { get; set; } = true;

    public Guid Version { get; set; }

    public List<Patient> Patients { get; init; } = new();

    public string ClientNumber => FormatNumber(Id);
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>"H. Clarke", as the Today and Schedule screens show owners.</summary>
    public string ShortName => FirstName.Length > 0 ? $"{FirstName[0]}. {LastName}" : LastName;

    public string Initials =>
        string.Concat(new[] { FirstName, LastName }.Where(n => n.Length > 0).Select(n => char.ToUpperInvariant(n[0])));

    public static string FormatNumber(int id) => $"CL-{id:D5}";

    public static string DigitsOf(string? phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        return digits.StartsWith("44") && digits.Length == 12 ? "0" + digits[2..] : digits;
    }

    /// <summary>Splits a name typed into one box ("Sam Taylor") into first name and surname.</summary>
    public static (string First, string Last) SplitName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => (string.Empty, string.Empty),
            1 => (string.Empty, parts[0]),
            _ => (string.Join(' ', parts[..^1]), parts[^1])
        };
    }
}
