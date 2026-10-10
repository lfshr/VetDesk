namespace VetDesk.Application.Common;

internal static class Text
{
    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool Has(string? value) => !string.IsNullOrWhiteSpace(value);
}
