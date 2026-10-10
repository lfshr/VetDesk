using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Patients;

public class Vaccination : BaseEntity
{
    public Patient Patient { get; set; } = null!;
    public required string VaccineName { get; set; }
    public DateTime GivenUtc { get; set; }
    public required string GivenBy { get; set; }
    public string? Batch { get; set; }
    public DateOnly? DueDate { get; set; }
    public int? ConsultationId { get; set; }

    public bool IsOverdue(DateOnly today) => DueDate is { } due && due < today;
}
