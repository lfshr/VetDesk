using VetDesk.Application.Common;
using VetDesk.Domain.Entities.Appointments;

namespace VetDesk.Application.Appointments;

public record AppointmentDto(
    int Id,
    DateTime StartUtc,
    int Minutes,
    int PatientId,
    string PatientName,
    string? Breed,
    string Species,
    int ClientId,
    string OwnerShortName,
    string OwnerPhone,
    string Reason,
    int TypeId,
    string TypeName,
    AppointmentKind Kind,
    AppointmentColour Colour,
    AppointmentStatus Status,
    int? ClinicianId,
    string? ClinicianName,
    DateTime? CheckedInUtc,
    bool SmsReminder,
    bool HasAllergy,
    int? ConsultationId,
    Guid Version,
    string? ApprovedBy = null)
{
    public DateTime EndUtc => StartUtc.AddMinutes(Minutes);

    public static AppointmentDto From(Appointment a, bool hasAllergy, int? consultationId) => new(
        a.Id, Utc.Mark(a.StartUtc), a.Minutes, a.Patient.Id, a.Patient.Name, a.Patient.Breed, a.Patient.Species,
        a.Patient.Client.Id, a.Patient.Client.ShortName, a.Patient.Client.Phone, a.Reason, a.Type.Id, a.Type.Name,
        a.Type.Kind, a.Type.Colour, a.Status, a.Clinician?.Id, a.Clinician?.ShortName, Utc.Mark(a.CheckedInUtc),
        a.SmsReminder, hasAllergy, consultationId, a.Version, a.ApprovedBy);
}

public record AppointmentTypeDto(int Id, string Name, AppointmentKind Kind, int Minutes, string SeenBy, bool OnlineBooking, AppointmentColour Colour, bool IsActive)
{
    public static AppointmentTypeDto From(AppointmentType t) => new(t.Id, t.Name, t.Kind, t.Minutes, t.SeenBy, t.OnlineBooking, t.Colour, t.IsActive);
}
