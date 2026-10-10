using System.Text;
using VetDesk.Application.Reports;

namespace VetDesk.UI.Services;

public static class Csv
{
    public static string From(ReportTable table)
    {
        var sb = new StringBuilder();
        Line(sb, table.Columns);
        foreach (var row in table.Rows)
            Line(sb, row);
        if (table.Totals is { } totals)
            Line(sb, totals);
        return sb.ToString();
    }

    public static string From(IReadOnlyList<string> columns, IEnumerable<IReadOnlyList<string>> rows) =>
        From(new ReportTable("", "", columns, rows.ToList(), null));

    private static void Line(StringBuilder sb, IEnumerable<string> cells)
    {
        sb.AppendJoin(',', cells.Select(Cell));
        sb.Append("\r\n");
    }

    private static string Cell(string value)
    {
        // A cell starting with = + - or @ would run as a formula when the file is opened in Excel.
        if (value.Length > 0 && "=+-@".Contains(value[0]) && !decimal.TryParse(value, out _))
            value = "'" + value;
        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}
