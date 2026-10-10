using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Domain.Entities.Consultations;

/// <summary>
/// The clinical record of one visit. Once completed it is locked: notes, vitals and charges can no longer change,
/// so the record matches what was invoiced and what was said at the time.
/// </summary>
public class Consultation : BaseAuditableEntity, IHasVersion
{
    public required Patient Patient { get; set; }
    public Appointment? Appointment { get; set; }
    public required StaffMember Clinician { get; set; }
    public DateTime StartedUtc { get; set; }
    public DateTime? CompletedUtc { get; private set; }
    public ConsultationStatus Status { get; private set; } = ConsultationStatus.Open;

    public string? Reason { get; set; }
    public string? Subjective { get; set; }
    public string? Objective { get; set; }
    public string? Assessment { get; set; }
    public string? Plan { get; set; }

    public decimal? TemperatureC { get; set; }
    public int? HeartRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public decimal? WeightKg { get; set; }
    public string? MucousMembranes { get; set; }
    public string? CapillaryRefill { get; set; }

    public List<ConsultationCharge> Charges { get; init; } = new();

    public int? InvoiceId { get; private set; }
    public Invoice? Invoice { get; private set; }

    public Guid Version { get; set; }

    public bool IsLocked => Status == ConsultationStatus.Completed;

    public bool SoapComplete =>
        !string.IsNullOrWhiteSpace(Subjective) && !string.IsNullOrWhiteSpace(Objective) &&
        !string.IsNullOrWhiteSpace(Assessment) && !string.IsNullOrWhiteSpace(Plan);

    public void MarkCompleted(DateTime nowUtc, Invoice? invoice)
    {
        Status = ConsultationStatus.Completed;
        CompletedUtc = nowUtc;
        Invoice = invoice;
    }
}

public enum ConsultationStatus
{
    Open,
    Completed
}
