using System.Globalization;
using VetDesk.Domain.Common;

namespace VetDesk.UI.Services;

/// <summary>Formatting as the design shows it: pounds, UK dates, 24-hour local times.</summary>
public static class Fmt
{
    public static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    public static string Money(decimal amount) => amount.ToString("C", Uk);

    public static string Qty(decimal quantity) => quantity.ToString("0.###", Uk);

    public static DateTime Local(DateTime utc) => ClinicTime.ToLocal(utc);

    public static string Time(DateTime utc) => Local(utc).ToString("HH:mm", Uk);

    public static string Date(DateTime utc) => Local(utc).ToString("d MMM yyyy", Uk);

    public static string Date(DateOnly date) => date.ToString("d MMM yyyy", Uk);

    public static string DateTime(DateTime utc) => Local(utc).ToString("d MMM yyyy, HH:mm", Uk);

    public static string LongDate(DateOnly date) => date.ToString("dddd d MMMM yyyy", Uk);

    public static string Ago(DateTime utc, DateTime nowUtc)
    {
        var span = nowUtc - utc;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} h ago";
        if (span.TotalDays < 2) return "yesterday";
        return Date(utc);
    }

    public static string Hours(TimeOnly? open, TimeOnly? close) =>
        open is { } o && close is { } c ? $"Open {o:HH\\:mm} – {c:HH\\:mm}" : "Closed today";
}
