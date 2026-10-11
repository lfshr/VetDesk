using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Domain.Entities.Appointments;

public class Appointment : BaseAuditableEntity, IHasVersion
{
    public required Patient Patient { get; set; }
    public required AppointmentType Type { get; set; }
    public StaffMember? Clinician { get; set; }

    public DateTime StartUtc { get; set; }
    public int Minutes { get; set; }
    public required string Reason { get; set; }

    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Booked;
    public DateTime? CheckedInUtc { get; private set; }

    /// <summary>The owner asked for a text reminder. Recorded only: SMS is not connected in v1.</summary>
    public bool SmsReminder { get; set; }

    /// <summary>
    /// The vet or administrator who approved a closed-day, out-of-hours or busy booking made by someone who can't book
    /// those times alone. Null when no approval was needed.
    /// </summary>
    public string? ApprovedBy { get; set; }

    public Guid Version { get; set; }

    public DateTime EndUtc => StartUtc.AddMinutes(Minutes);

    public bool IsOpen => Status is not (AppointmentStatus.Done or AppointmentStatus.Cancelled or AppointmentStatus.NoShow);

    public bool Overlaps(DateTime startUtc, DateTime endUtc) => StartUtc < endUtc && startUtc < EndUtc;

    /// <summary>
    /// Moves the appointment through the day. Returns false when the move isn't allowed from the current status,
    /// so the caller can report it rather than throw.
    /// </summary>
    public bool ChangeStatus(AppointmentStatus next, DateTime nowUtc)
    {
        if (!CanMoveTo(next))
            return false;

        if (next == AppointmentStatus.CheckedIn)
            CheckedInUtc = nowUtc;
        if (next == AppointmentStatus.Booked)
            CheckedInUtc = null;

        Status = next;
        return true;
    }

    public bool CanMoveTo(AppointmentStatus next) => (Status, next) switch
    {
        (AppointmentStatus.Booked, AppointmentStatus.CheckedIn) => true,
        (AppointmentStatus.Booked, AppointmentStatus.InConsult) => true,
        (AppointmentStatus.Booked, AppointmentStatus.Cancelled) => true,
        (AppointmentStatus.Booked, AppointmentStatus.NoShow) => true,
        (AppointmentStatus.CheckedIn, AppointmentStatus.InConsult) => true,
        (AppointmentStatus.CheckedIn, AppointmentStatus.Booked) => true,
        (AppointmentStatus.CheckedIn, AppointmentStatus.Cancelled) => true,
        (AppointmentStatus.InConsult, AppointmentStatus.Done) => true,
        (AppointmentStatus.NoShow, AppointmentStatus.Booked) => true,
        _ => false
    };
}

public enum AppointmentStatus
{
    Booked,
    CheckedIn,
    InConsult,
    Done,
    Cancelled,
    NoShow
}

public static class AppointmentStatusLabels
{
    /// <summary>How the screens and reports name a status; surgery has its own words for the same steps.</summary>
    public static string Label(this AppointmentStatus status, AppointmentKind kind) => status switch
    {
        AppointmentStatus.InConsult => kind == AppointmentKind.Surgery ? "In theatre" : "In consult",
        AppointmentStatus.CheckedIn => kind == AppointmentKind.Surgery ? "Admitted" : "Waiting",
        AppointmentStatus.NoShow => "No-show",
        AppointmentStatus.Booked when kind == AppointmentKind.Surgery => "Surgery",
        _ => status.ToString()
    };
}
