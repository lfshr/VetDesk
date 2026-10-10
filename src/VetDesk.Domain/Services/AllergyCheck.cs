using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Products;

namespace VetDesk.Domain.Services;

/// <summary>
/// Flags products a patient may react to. A product conflicts with an allergy when its name or listed
/// ingredients mention the substance, or when both are in the same drug class (amoxicillin allergy flags every
/// penicillin-class product, as the design's prescribing note describes).
/// </summary>
public static class AllergyCheck
{
    public static IReadOnlyList<PatientAllergy> Conflicts(Product product, IEnumerable<PatientAllergy> allergies) =>
        allergies.Where(a => ConflictsWith(product, a)).ToList();

    public static bool ConflictsWith(Product product, PatientAllergy allergy)
    {
        var substance = allergy.Substance.Trim();
        if (substance.Length > 0 &&
            (Contains(product.Name, substance) || Contains(product.Ingredients, substance)))
            return true;

        return !string.IsNullOrWhiteSpace(allergy.DrugClass) && !string.IsNullOrWhiteSpace(product.DrugClass) &&
               string.Equals(allergy.DrugClass.Trim(), product.DrugClass.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string? text, string value) =>
        text is not null && text.Contains(value, StringComparison.OrdinalIgnoreCase);

    public static string Describe(Product product, IEnumerable<PatientAllergy> conflicts) =>
        $"{product.Name} conflicts with {string.Join(", ", conflicts.Select(c => c.Label.ToLowerInvariant()))}";
}
