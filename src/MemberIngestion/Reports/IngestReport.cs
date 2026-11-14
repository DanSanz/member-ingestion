using MemberIngestion.Ingestion;
using Scheme.Common.Dates;

namespace MemberIngestion.Reports;

/// <summary>
/// The per-file report Employer Relations send back to the employer. Text only; the numbers come from
/// <see cref="IngestSummary"/>. Keep logic out of here.
/// </summary>
public static class IngestReport
{
    public static string For(IngestSummary summary) =>
        $"""
        Member file {summary.FileId} from employer {summary.EmployerId}, dated {summary.FileDate.ToUkString()}
        Rows read: {summary.RowsRead}
        Rows loaded: {summary.Loaded}
        """;
}
