namespace MemberIngestion.Reports;

/// <summary>Report rows as CSV text, for Employer Relations to open in a spreadsheet.</summary>
public static class ReportCsv
{
    private const string LineBreak = "\r\n";

    public static string Line(params string[] cells) => string.Join(",", cells.Select(Cell));

    public static string Table(IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string>> rows) =>
        string.Concat(new[] { header }.Concat(rows).Select(row => Line([.. row]) + LineBreak));

    private static string Cell(string cell) =>
        cell.IndexOfAny([',', '"', '\r', '\n']) < 0
            ? cell
            : $"\"{cell.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
