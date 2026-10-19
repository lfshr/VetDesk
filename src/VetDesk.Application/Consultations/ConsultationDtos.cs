using VetDesk.Domain.Entities.Consultations;
using VetDesk.Domain.Entities.Products;

namespace VetDesk.Application.Consultations;

/// <param name="ProductId">Null for a free-text line.</param>
/// <param name="ListUnitPrice">The net list price, to compare a changed price with; null for a free-text line.</param>
public record ChargeDto(
    int Id,
    int? ProductId,
    string Description,
    ProductCategory? Category,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    VatRate VatRate,
    decimal Net,
    bool IsPrescription,
    string? Directions,
    bool IsControlledDrug,
    string? AllergyOverrideReason,
    IReadOnlyList<string> AllergyConflicts,
    decimal? ListUnitPrice);

public record ConsultationDetail(
    int Id,
    int PatientId,
    string PatientName,
    string PatientNumber,
    string SpeciesAndBreed,
    string Age,
    string OwnerName,
    int ClientId,
    IReadOnlyList<string> Allergies,
    IReadOnlyList<(string Text, Domain.Entities.Patients.AlertSeverity Severity)> Alerts,
    int? AppointmentId,
    string? Reason,
    string ClinicianName,
    DateTime StartedUtc,
    DateTime? CompletedUtc,
    ConsultationStatus Status,
    string? Notes,
    string? OwnerAdvice,
    decimal? TemperatureC,
    int? HeartRate,
    int? RespiratoryRate,
    decimal? WeightKg,
    string? MucousMembranes,
    string? CapillaryRefill,
    IReadOnlyList<ChargeDto> Charges,
    decimal Net,
    decimal Vat,
    decimal Total,
    int? InvoiceId,
    string? InvoiceNumber,
    Guid Version)
{
    public bool IsLocked => Status == ConsultationStatus.Completed;

    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    public bool HasUnresolvedAllergyConflict => Charges.Any(c => c.AllergyConflicts.Count > 0 && c.AllergyOverrideReason is null);
}
