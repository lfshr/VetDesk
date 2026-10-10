using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Appointments;

/// <summary>A bookable kind of appointment, set up in Settings › Appointment types.</summary>
public class AppointmentType : BaseEntity
{
    public required string Name { get; set; }
    public AppointmentKind Kind { get; set; }
    public int Minutes { get; set; }

    /// <summary>Who normally sees it: "Vet" or "Nurse". Informational in v1.</summary>
    public string SeenBy { get; set; } = "Vet";

    /// <summary>Kept for the design's "Online" switch. There is no client portal in v1, so nothing reads it yet.</summary>
    public bool OnlineBooking { get; set; }

    public AppointmentColour Colour { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>The two blocks the schedule distinguishes: consultations (teal) and surgery (purple).</summary>
public enum AppointmentKind
{
    Consultation,
    Surgery
}

public enum AppointmentColour
{
    Teal,
    Blue,
    Purple,
    Amber
}
