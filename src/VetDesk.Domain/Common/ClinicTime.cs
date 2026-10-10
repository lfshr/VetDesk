namespace VetDesk.Domain.Common;

/// <summary>
/// Times are stored in UTC and shown in UK local time. The practice is single-site in v1, so the zone is fixed.
/// </summary>
public static class ClinicTime
{
    public static readonly TimeZoneInfo Zone = FindZone();

    private static TimeZoneInfo FindZone()
    {
        // .NET on Windows uses ICU when available, which accepts IANA ids; keep the Windows id as a fallback.
        foreach (var id in new[] { "Europe/London", "GMT Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
                return zone;
        }

        return TimeZoneInfo.Utc;
    }

    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

    public static DateTime ToUtc(DateOnly date, TimeOnly time) => ToUtc(date.ToDateTime(time));

    /// <summary>The UTC range covering a local calendar day, for "what's on today" queries.</summary>
    public static (DateTime StartUtc, DateTime EndUtc) DayRange(DateOnly date) =>
        (ToUtc(date, TimeOnly.MinValue), ToUtc(date.AddDays(1), TimeOnly.MinValue));

    public static DateOnly Today(DateTime utcNow) => DateOnly.FromDateTime(ToLocal(utcNow));
}
