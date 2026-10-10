using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Patients;

/// <summary>A chip under the patient's name: handling warnings, medical alerts and notes.</summary>
public class PatientAlert : BaseEntity
{
    public Patient Patient { get; set; } = null!;
    public required string Text { get; set; }
    public AlertSeverity Severity { get; set; }

    /// <summary>The list entry it was picked from, or null when it was typed in.</summary>
    public string? PresetKey { get; set; }

    /// <summary>The pick-list in the design's alert editor.</summary>
    public static readonly IReadOnlyList<(string Key, string Text, AlertSeverity Severity)> Presets =
    [
        ("0", "Aggressive — handle with care", AlertSeverity.Alert),
        ("1", "Bites when restrained", AlertSeverity.Alert),
        ("2", "Diabetic — on insulin", AlertSeverity.Alert),
        ("3", "Epileptic", AlertSeverity.Alert),
        ("4", "Heart murmur — anaesthetic risk", AlertSeverity.Alert),
        ("5", "Nervous — muzzle for nail clips", AlertSeverity.Warning),
        ("6", "Brachycephalic — airway risk", AlertSeverity.Warning),
        ("7", "Escape risk — keep doors closed", AlertSeverity.Warning),
        ("8", "Payment required upfront", AlertSeverity.Warning),
        ("9", "Insured", AlertSeverity.Note),
        ("10", "Prefers a female vet", AlertSeverity.Note),
        ("11", "Owner hard of hearing", AlertSeverity.Note),
        ("12", "Microchip details need updating", AlertSeverity.Note),
    ];
}

/// <summary>Ordered most serious first, which is how the chips are sorted.</summary>
public enum AlertSeverity
{
    Alert,
    Warning,
    Note
}
