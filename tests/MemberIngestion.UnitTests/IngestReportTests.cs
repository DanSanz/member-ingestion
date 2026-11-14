using MemberIngestion.Ingestion;
using MemberIngestion.Reports;

namespace MemberIngestion.UnitTests;

public sealed class IngestReportTests
{
    [Fact]
    public void Report_gives_the_counts_and_the_file_date_day_first()
    {
        var text = IngestReport.For(new IngestSummary("SYNTH-M-E90001-20261102", "E90001", new DateOnly(2026, 11, 2), 8, 8));
        Assert.Contains("dated 02/11/2026", text, StringComparison.Ordinal);
        Assert.Contains("Rows loaded: 8", text, StringComparison.Ordinal);
    }
}
