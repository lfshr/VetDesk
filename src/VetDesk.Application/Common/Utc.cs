namespace VetDesk.Application.Common;

internal static class Utc
{
    // SQL Server and SQLite hand back DateTimeKind.Unspecified; everything stored is UTC, so say so.
    public static DateTime Mark(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static DateTime? Mark(DateTime? value) => value is { } v ? Mark(v) : null;
}
