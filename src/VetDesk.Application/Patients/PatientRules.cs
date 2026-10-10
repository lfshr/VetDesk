using VetDesk.Application.Common;
using VetDesk.Domain.Entities.Patients;

namespace VetDesk.Application.Patients;

internal static class PatientRules
{
    public static string? Validate(string name, string species, DateOnly? dateOfBirth, DateOnly today)
    {
        if (!Text.Has(name))
            return "Enter the pet's name.";
        if (!Text.Has(species))
            return "Choose a species.";
        if (dateOfBirth is { } dob && dob > today)
            return "Date of birth can't be in the future.";
        return null;
    }

    /// <summary>Microchips are 15 digits; stored without spaces so search finds them however they were typed.</summary>
    public static string? CleanMicrochip(string? chip) =>
        Text.Clean(chip)?.Replace(" ", string.Empty);

    public static string CleanSpecies(string species)
    {
        var trimmed = species.Trim();
        return Patient.SpeciesOptions.FirstOrDefault(s => s.Equals(trimmed, StringComparison.OrdinalIgnoreCase)) ?? trimmed;
    }
}
