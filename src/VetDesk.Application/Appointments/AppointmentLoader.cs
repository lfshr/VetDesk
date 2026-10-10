using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Appointments;

namespace VetDesk.Application.Appointments;

internal static class AppointmentLoader
{
    public static async Task<IReadOnlyList<AppointmentDto>> LoadAsync(
        IVetDeskDbContext db, Func<IQueryable<Appointment>, IQueryable<Appointment>> filter, CancellationToken cancellationToken)
    {
        var appointments = await filter(db.Appointments.AsNoTracking()
                .Include(a => a.Patient).ThenInclude(p => p.Client)
                .Include(a => a.Patient).ThenInclude(p => p.Allergies)
                .Include(a => a.Type)
                .Include(a => a.Clinician))
            .OrderBy(a => a.StartUtc)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var ids = appointments.Select(a => a.Id).ToList();
        var consultations = await db.Consultations.AsNoTracking()
            .Where(c => c.Appointment != null && ids.Contains(c.Appointment.Id))
            .Select(c => new { AppointmentId = c.Appointment!.Id, c.Id })
            .ToListAsync(cancellationToken);
        var byAppointment = consultations.GroupBy(c => c.AppointmentId).ToDictionary(g => g.Key, g => g.Max(c => c.Id));

        return appointments
            .Select(a => AppointmentDto.From(a, a.Patient.Allergies.Count > 0, byAppointment.TryGetValue(a.Id, out var c) ? c : null))
            .ToList();
    }
}
