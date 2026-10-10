using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Appointments;

namespace VetDesk.Application.Appointments;

/// <summary>
/// The schedule allows several appointments at once (more than one vet works), so a clash only means the same
/// clinician or the same patient booked twice.
/// </summary>
internal static class AppointmentClashes
{
    public static async Task<string?> FindAsync(
        IVetDeskDbContext db, int patientId, int? clinicianId, DateTime startUtc, DateTime endUtc, int? ignoreId,
        CancellationToken cancellationToken)
    {
        // Overlap is checked in memory on the day's appointments; EF can't translate EndUtc (a computed property).
        var candidates = await db.Appointments.AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Clinician)
            .Where(a => a.Id != ignoreId && a.StartUtc < endUtc && a.StartUtc > startUtc.AddHours(-24))
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
            .ToListAsync(cancellationToken);

        var overlapping = candidates.Where(a => a.Overlaps(startUtc, endUtc)).ToList();

        var samePatient = overlapping.FirstOrDefault(a => a.Patient.Id == patientId);
        if (samePatient is not null)
            return $"{samePatient.Patient.Name} is already booked at that time.";

        if (clinicianId is not null && overlapping.FirstOrDefault(a => a.Clinician?.Id == clinicianId) is { } busy)
            return $"{busy.Clinician!.ShortName} already has an appointment at that time.";

        return null;
    }
}
