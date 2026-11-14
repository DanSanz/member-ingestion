using MemberIngestion.Feeds;
using Scheme.Common.Feeds;

namespace MemberIngestion.ModuleTests;

public sealed class MemberFileIngestorTests : IDisposable
{
    private readonly IngestionHarness harness = new();

    public void Dispose() => harness.Dispose();

    [Fact]
    public void Loads_every_row_of_a_valid_file()
    {
        var summary = harness.Ingestor.IngestFile(Fixtures.PathTo("members_E90001_20261102.txt"));
        Assert.Equal((8, 8), (summary.RowsRead, summary.Loaded));
        Assert.Equal(8, harness.Store.All().Count);
        Assert.Equal("13/10/1992", harness.Store.Find("M900108")!.DateOfBirth);
    }

    [Fact]
    public void Audits_the_load_with_the_job_actor()
    {
        harness.Ingestor.IngestFile(Fixtures.PathTo("members_E90001_20261102.txt"));
        var loaded = Assert.Single(harness.Audit.OfType("member_file_loaded"));
        Assert.Equal("SYNTH-M-E90001-20261102", loaded.SubjectRef);
        Assert.Equal(harness.Options.AuditActor, loaded.Actor);
    }

    [Fact]
    public void Logs_counts_and_the_file_id_but_no_personal_data()
    {
        harness.Ingestor.IngestFile(Fixtures.PathTo("members_E90001_20261102.txt"));
        Assert.Contains("Loaded 8 of 8 member rows from SYNTH-M-E90001-20261102", harness.Logs.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("QQ100101A", harness.Logs.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("14/03/1981", harness.Logs.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("contributions_E90001_202609.txt")]
    [InlineData("members_E90001_extra_column.txt")]
    [InlineData("members_E90002_bad_trailer.txt")]
    public void Rejects_a_file_that_breaks_the_envelope_or_the_contract_and_loads_nothing(string fileName)
    {
        Assert.Throws<FeedFormatException>(() => harness.Ingestor.IngestFile(Fixtures.PathTo(fileName)));
        Assert.Empty(harness.Store.All());
        Assert.Empty(harness.Audit.Events);
    }

    [Fact]
    public void Accepts_an_extra_column_when_strict_schema_is_off()
    {
        using var lax = new IngestionHarness(strictSchema: false);
        lax.Ingestor.IngestFile(Fixtures.PathTo("members_E90001_extra_column.txt"));
        Assert.Equal(3, lax.Store.All().Count);
    }

    [Fact]
    public void Loads_nothing_from_an_empty_member_file_and_does_not_reject_it()
    {
        var summary = harness.Ingestor.IngestFile(Fixtures.PathTo("no_members_E90009_20261102.txt"));
        Assert.Equal((0, 0), (summary.RowsRead, summary.Loaded));
        Assert.Empty(harness.Store.All());
    }
}

public sealed class MemberFeedReaderTests : IDisposable
{
    private readonly IngestionHarness harness = new();

    public void Dispose() => harness.Dispose();

    [Fact]
    public void Reads_one_record_per_detail_line_in_file_order()
    {
        var file = harness.Get<MemberFeedReader>().Read(Fixtures.PathTo("members_E90001_20261102.txt"));
        Assert.Equal(
            ["M900101", "M900102", "M900103", "M900104", "M900105", "M900106", "M900107", "M900108"],
            file.Rows.Select(row => row.MemberRef));
    }

    [Theory]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    public void File_date_is_read_day_first_whatever_the_machine_culture(string culture)
    {
        using var _ = new CultureScope(culture);
        var file = harness.Get<MemberFeedReader>().Read(Fixtures.PathTo("members_E90001_20261102.txt"));
        Assert.Equal(new DateOnly(2026, 11, 2), file.Header.FileDate);
    }
}
