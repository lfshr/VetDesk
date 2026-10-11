using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Staff;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.Appointments;

/// <summary>
/// Closed days, out-of-hours times and busy times can still be booked, but only by a vet or administrator. Anyone
/// else (nurses, reception) needs one of them to approve at the same desk with their own PIN, and their name goes
/// on the appointment, as a controlled drug witness's does on the register.
/// </summary>
internal static class BookingApproval
{
    /// <summary>Appointments already booked over a time that make it busy, as hatched in the booking grid.</summary>
    public const int BusyFrom = 2;

    public const string Ask = "A vet or administrator needs to approve this booking.";

    /// <summary>Why booking this time needs approval, or null when it doesn't.</summary>
    public static async Task<string?> WhyNeededAsync(
        IVetDeskDbContext db, DateTime startUtc, int minutes, int? ignoreId, CancellationToken cancellationToken)
    {
        var endUtc = startUtc.AddMinutes(minutes);
        var start = ClinicTime.ToLocal(startUtc);
        var end = ClinicTime.ToLocal(endUtc);
        var date = DateOnly.FromDateTime(start);
        var reasons = new List<string>();

        var settings = await db.ClinicSettings.AsNoTracking().FirstAsync(cancellationToken);
        if (settings.HoursOn(date.DayOfWeek) is not { } hours)
            reasons.Add($"The practice is closed on {date.DayOfWeek}s.");
        else if (TimeOnly.FromDateTime(start) < hours.Open || end > date.ToDateTime(hours.Close))
            reasons.Add($"{start:HH:mm}–{end:HH:mm} is outside opening hours ({hours.Open:HH:mm}–{hours.Close:HH:mm}).");

        // Counted as the booking grid counts a slot: open appointments overlapping the whole of this one.
        var candidates = await db.Appointments.AsNoTracking()
            .Where(a => a.Id != ignoreId && a.StartUtc < endUtc && a.StartUtc > startUtc.AddHours(-24))
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
            .Select(a => new { a.StartUtc, a.Minutes })
            .ToListAsync(cancellationToken);
        var booked = candidates.Count(a => Utc.Mark(a.StartUtc) < endUtc && startUtc < Utc.Mark(a.StartUtc).AddMinutes(a.Minutes));
        if (booked >= BusyFrom)
            reasons.Add($"{booked} appointments are already booked at {start:HH:mm}.");

        return reasons.Count == 0 ? null : string.Join(" ", reasons);
    }

    /// <remarks>Call before the request changes anything: a wrong PIN is counted and saved straight away.</remarks>
    public static async Task<(StaffMember? Approver, string? Problem)> VerifyAsync(
        IVetDeskDbContext db, IPasswordHasher hasher, IDevicePinLimit deviceLimit, ICurrentUser user, int approverId, string? pin,
        DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (approverId == user.StaffId)
            return (null, "Someone else must approve the booking.");

        var approver = await db.StaffMembers.FirstOrDefaultAsync(s => s.Id == approverId && s.IsActive, cancellationToken);
        if (approver is null)
            return (null, "That person isn't an active member of staff.");
        if (!approver.CanApproveBookings)
            return (null, $"{approver.ShortName} can't approve bookings. Ask a vet or administrator.");

        if (await StaffRules.CheckPinAsync(db, hasher, deviceLimit, approver, pin, nowUtc, cancellationToken) is { } problem)
            return (null, problem);

        return (approver, null);
    }
}
