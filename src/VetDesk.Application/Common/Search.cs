namespace VetDesk.Application.Common;

internal static class Search
{
    public const string EscapeText = "\\";

    /// <summary>
    /// A LIKE pattern matching <paramref name="text"/> anywhere. LIKE is used rather than Contains because it is
    /// case-insensitive on both SQL Server (default collation) and SQLite.
    /// </summary>
    public static string Anywhere(string text) =>
        "%" + text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[") + "%";
}
