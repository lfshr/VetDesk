using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Clients;

namespace VetDesk.Domain.Entities.Patients;

public class Patient : BaseAuditableEntity, IHasVersion
{
    public required string Name { get; set; }
    public required Client Client { get; set; }
    public required string Species { get; set; }
    public string? Breed { get; set; }
    public Sex Sex { get; set; }
    public bool IsNeutered { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Microchip { get; set; }
    public bool IsDeceased { get; set; }

    public Guid Version { get; set; }

    public List<PatientAlert> Alerts { get; init; } = new();
    public List<PatientAllergy> Allergies { get; init; } = new();

    public string PatientNumber => FormatNumber(Id);

    public static string FormatNumber(int id) => $"PT-{id:D6}";

    public static readonly IReadOnlyList<string> SpeciesOptions = ["Dog", "Cat", "Rabbit", "Guinea pig", "Other"];

    public string SpeciesAndBreed => string.IsNullOrWhiteSpace(Breed) ? Species : $"{Species} · {Breed}";

    public string SexLabel => (Sex, IsNeutered) switch
    {
        (Sex.Male, true) => "Male, neutered",
        (Sex.Male, false) => "Male, entire",
        (Sex.Female, true) => "Female, spayed",
        (Sex.Female, false) => "Female, entire",
        _ => "Sex not recorded"
    };

    /// <summary>"6 yrs", "6 yrs 2 mths", "10 wks", as the design shows ages.</summary>
    public string AgeLabel(DateOnly today, bool withMonths = false)
    {
        if (DateOfBirth is not { } dob || dob > today)
            return "Age not set";

        var months = (today.Year - dob.Year) * 12 + today.Month - dob.Month - (today.Day < dob.Day ? 1 : 0);
        if (months < 4)
            return $"{(today.DayNumber - dob.DayNumber) / 7} wks";
        if (months < 12)
            return $"{months} mths";

        var years = months / 12;
        var rest = months % 12;
        var yearsText = years == 1 ? "1 yr" : $"{years} yrs";
        return withMonths && rest > 0 ? $"{yearsText} {rest} {(rest == 1 ? "mth" : "mths")}" : yearsText;
    }
}

public enum Sex
{
    Unknown,
    Male,
    Female
}
