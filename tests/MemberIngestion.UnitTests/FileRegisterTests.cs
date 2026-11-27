using MemberIngestion.Ingestion;
using Scheme.Common.Dates;
using Scheme.Common.Feeds;

namespace MemberIngestion.UnitTests;

public sealed class FileRegisterTests
{
    private static FeedHeader Header(string employerId, string fileId, DateOnly fileDate) =>
        new("MEMBERS", employerId, fileId, fileDate, "3.1");

    private static string[] HeaderFields(string fixture) =>
        File.ReadLines(Fixtures.PathTo(fixture)).First().Split(FeedReader.Separator);

    [Fact]
    public void A_receipt_carries_the_file_id_the_employer_and_the_file_date()
    {
        var receipt = new FileRegister().Record(FeedReader.ReadHeader(Fixtures.PathTo("members_E90001_20261102.txt")));
        var hdr = HeaderFields("members_E90001_20261102.txt");
        Assert.Equal(
            (hdr[3], hdr[2], UkDate.Parse(hdr[4])),
            (receipt.FileId, receipt.EmployerId, receipt.FileDate));
    }

    [Fact]
    public void Latest_for_an_employer_is_the_last_file_recorded_for_it()
    {
        var register = new FileRegister();
        register.Record(Header("E90001", "SYNTH-M-E90001-20261002", new DateOnly(2026, 10, 2)));
        register.Record(FeedReader.ReadHeader(Fixtures.PathTo("members_E90001_extra_column.txt")));
        register.Record(FeedReader.ReadHeader(Fixtures.PathTo("members_E90002_bad_trailer.txt")));
        var hdr = HeaderFields("members_E90001_extra_column.txt");
        var latest = register.LatestFor("E90001")!;
        Assert.Equal((hdr[3], UkDate.Parse(hdr[4])), (latest.FileId, latest.FileDate));
    }

    [Fact]
    public void A_register_opened_on_the_same_file_knows_what_an_earlier_run_recorded()
    {
        var path = Path.Combine(Path.GetTempPath(), $"SYNTH-{Guid.NewGuid():N}.json");
        try
        {
            new FileRegister(path).Record(Header("E90001", "SYNTH-M-E90001-20261102", new DateOnly(2026, 11, 2)));
            Assert.Equal("SYNTH-M-E90001-20261102", new FileRegister(path).LatestFor("E90001")!.FileId);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
