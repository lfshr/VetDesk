using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Consultations;

namespace VetDesk.Application.Common;

internal static class LastVisits
{
    /// <summary>The start of each patient's most recent consultation.</summary>
    public static async Task<Dictionary<int, DateTime>> ForPatientsAsync(IVetDeskDbContext db, IReadOnlyCollection<int>? patientIds, CancellationToken cancellationToken)
    {
        var query = db.Consultations.AsNoTracking();
        if (patientIds is not null)
            query = query.Where(c => patientIds.Contains(c.Patient.Id));

        var rows = await query
            .GroupBy(c => c.Patient.Id)
            .Select(g => new { PatientId = g.Key, Last = g.Max(c => c.StartedUtc) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.PatientId, r => Utc.Mark(r.Last));
    }
}
