using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Patients;

/// <summary>
/// A drug allergy. Kept apart from free-text alerts so prescribing and invoicing can check products against it.
/// </summary>
public class PatientAllergy : BaseEntity
{
    public Patient Patient { get; set; } = null!;

    /// <summary>What the patient reacts to, as the vet wrote it, e.g. "amoxicillin".</summary>
    public required string Substance { get; set; }

    /// <summary>Optional drug class to flag related products too, e.g. "Penicillin".</summary>
    public string? DrugClass { get; set; }

    public string Label => $"Allergy: {Substance}";
}
