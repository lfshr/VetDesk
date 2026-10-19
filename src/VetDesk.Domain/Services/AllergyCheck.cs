using VetDesk.Domain.Entities.Consultations;
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

    public static string Describe(Product product, IEnumerable<PatientAllergy> conflicts) => Describe(product.Name, conflicts);

    /// <summary>
    /// For a free-text charge there's no product to check, only what was typed: it conflicts when it names an allergy's
    /// substance. Drug classes can't be caught this way.
    /// </summary>
    public static IReadOnlyList<PatientAllergy> Conflicts(string description, IEnumerable<PatientAllergy> allergies) =>
        allergies.Where(a => Mentions(description, a.Substance)).ToList();

    /// <summary>A consultation line's conflicts: its product's, or for a free-text line, what its description names.</summary>
    public static IReadOnlyList<PatientAllergy> Conflicts(ConsultationCharge charge, IEnumerable<PatientAllergy> allergies) =>
        charge.Product is { } product ? Conflicts(product, allergies) : Conflicts(charge.Description, allergies);

    /// <summary>Whether <paramref name="description"/> names <paramref name="substance"/>, for checking as it's typed.</summary>
    public static bool Mentions(string? description, string substance) =>
        substance.Trim().Length > 0 && Contains(description, substance.Trim());

    public static string Describe(string name, IEnumerable<PatientAllergy> conflicts) =>
        $"{name} conflicts with {string.Join(", ", conflicts.Select(c => c.Label.ToLowerInvariant()))}";
}
