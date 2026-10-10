using VetDesk.Domain.Entities.Patients;

namespace VetDesk.Application.Patients;

public record PatientRow(
    int Id,
    string Name,
    string PatientNumber,
    string Species,
    string SpeciesAndBreed,
    string Age,
    int ClientId,
    string OwnerName,
    string OwnerPhone,
    string? Microchip,
    DateTime? LastVisitUtc,
    decimal Balance,
    bool IsDeceased,
    IReadOnlyList<string> AllergyLabels);

public record AlertDto(int Id, string Text, AlertSeverity Severity, string? PresetKey);

public record AllergyDto(int Id, string Substance, string? DrugClass, string Label);

public record WeightDto(DateTime RecordedUtc, decimal Kg, string Context);

public record VaccinationDto(int Id, string VaccineName, DateTime GivenUtc, string GivenBy, string? Batch, DateOnly? DueDate, bool IsOverdue, bool IsDueSoon);

public record MedicationDto(string ProductName, decimal Quantity, string Unit, string? Directions, DateTime DispensedUtc, string PrescribedBy);

public record AdministeredDrug(string Name, string Quantity, bool IsControlledDrug);

public enum HistoryKind { Consultation, Surgery, Vaccination, Note }

public record HistoryEntry(
    HistoryKind Kind,
    DateTime Utc,
    string Title,
    string Author,
    string? Body,
    decimal? WeightKg,
    int? ConsultationId,
    bool IsOpen,
    IReadOnlyList<AdministeredDrug> Drugs);

public record PrescriptionDto(int Id, string ProductName, decimal Quantity, string Unit, string? Directions, string PrescribedBy, DateTime CreatedUtc, string Status);

public record PatientInvoiceDto(int Id, string Number, DateTime IssuedUtc, decimal Total, decimal AmountDue, string Status);

public record PatientOwner(
    int Id,
    string ClientNumber,
    string FullName,
    string Phone,
    string? Email,
    string? Address,
    string? Postcode,
    bool RemindersOptIn,
    decimal Balance,
    IReadOnlyList<(int Id, string Name, string SpeciesAndBreed)> Pets,
    int? OwedInvoiceId = null,
    string? OwedInvoiceNumber = null);

public record PatientDetail(
    int Id,
    string Name,
    string PatientNumber,
    string Species,
    string? Breed,
    string SpeciesAndBreed,
    Sex Sex,
    bool IsNeutered,
    string SexLabel,
    DateOnly? DateOfBirth,
    string Age,
    string? Microchip,
    bool IsDeceased,
    decimal? LatestWeightKg,
    PatientOwner Owner,
    IReadOnlyList<AlertDto> Alerts,
    IReadOnlyList<AllergyDto> Allergies,
    IReadOnlyList<HistoryEntry> History,
    IReadOnlyList<WeightDto> Weights,
    IReadOnlyList<VaccinationDto> Vaccinations,
    IReadOnlyList<MedicationDto> CurrentMedication,
    IReadOnlyList<PrescriptionDto> Prescriptions,
    IReadOnlyList<PatientInvoiceDto> Invoices,
    int? OpenConsultationId,
    Guid Version);
