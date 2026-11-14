using Scheme.Common.Audit;
using Scheme.Common.Dates;
using Scheme.Common.Feeds;

namespace MemberIngestion.UnitTests;

public sealed class UkDateTests
{
    [Theory]
    [InlineData("14/03/1981", 1981, 3, 14)]
    [InlineData("29/02/1988", 1988, 2, 29)]
    [InlineData("01/01/1900", 1900, 1, 1)]
    public void Parses_day_first(string text, int year, int month, int day) =>
        Assert.Equal(new DateOnly(year, month, day), UkDate.Parse(text));

    [Theory]
    [InlineData("31/02/1980")]
    [InlineData("29/02/1981")]
    [InlineData("03/14/1981")]
    [InlineData("1981-03-14")]
    [InlineData("14/03/81")]
    [InlineData("4/03/1981")]
    [InlineData(" 14/03/1981")]
    [InlineData("14.03.1981")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_anything_that_is_not_a_real_dd_mm_yyyy_date(string? text) =>
        Assert.False(UkDate.TryParse(text, out _));

    [Fact]
    public void Error_message_does_not_repeat_the_input()
    {
        var error = Assert.Throws<InvalidDateException>(() => UkDate.Parse("31/02/1980"));
        Assert.DoesNotContain("31/02/1980", error.Message);
    }

    [Theory]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void Machine_culture_makes_no_difference(string culture)
    {
        using var _ = new CultureScope(culture);
        Assert.Equal(new DateOnly(2026, 11, 2), UkDate.Parse("02/11/2026"));
        Assert.Equal("02/11/2026", new DateOnly(2026, 11, 2).ToUkString());
    }

    [Theory]
    [InlineData("14/03/1981", "13/03/1997", 15)]
    [InlineData("14/03/1981", "14/03/1997", 16)]
    [InlineData("29/02/1988", "28/02/2005", 16)]
    [InlineData("29/02/1988", "01/03/2005", 17)]
    public void Age_on_a_date(string born, string on, int age) =>
        Assert.Equal(age, UkDate.AgeOn(UkDate.Parse(born), UkDate.Parse(on)));
}

public sealed class AuditLogTests
{
    [Fact]
    public void Records_actor_and_rule_id()
    {
        var audit = new InMemoryAuditLog();
        var recorded = audit.Record("member_file_loaded", "SYNTH-M-E90001-20261102", "8 of 8 rows", actor: "svc-test", ruleId: "R-1");
        Assert.Equal(recorded, Assert.Single(audit.Events));
        Assert.Equal(("svc-test", "R-1"), (recorded.Actor, recorded.RuleId));
    }

    [Theory]
    [InlineData("", "M900001")]
    [InlineData("member_file_loaded", " ")]
    public void Event_type_and_subject_are_required(string eventType, string subjectRef) =>
        Assert.ThrowsAny<ArgumentException>(() => new InMemoryAuditLog().Record(eventType, subjectRef, "detail"));

    [Fact]
    public void Json_lines_log_appends_one_line_per_event()
    {
        var path = Path.Combine(Path.GetTempPath(), $"audit-{Guid.NewGuid():N}.jsonl");
        try
        {
            var audit = new JsonLinesAuditLog(path);
            audit.Record("member_file_loaded", "SYNTH-M-E90001-20261102", "8 of 8 rows", actor: "svc-test");
            audit.Record("member_file_loaded", "SYNTH-M-E90002-20261102", "3 of 3 rows", actor: "svc-test", ruleId: "R-1");
            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.Contains("\"ruleId\":\"R-1\"", lines[1], StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

public sealed class FeedReaderTests
{
    [Fact]
    public void Reads_the_header()
    {
        var header = FeedReader.ReadHeader(Fixtures.PathTo("members_E90001_20261102.txt"));
        Assert.Equal(new FeedHeader("MEMBERS", "E90001", "SYNTH-M-E90001-20261102", new DateOnly(2026, 11, 2), "3.1"), header);
    }

    [Fact]
    public void Trims_every_field()
    {
        var file = FeedReader.Read(Fixtures.PathTo("members_E90001_20261102.txt"));
        Assert.Equal("13/10/1992", file.Details[7][5]);
    }

    [Fact]
    public void Trailer_count_must_match_the_detail_lines()
    {
        var error = Assert.Throws<FeedFormatException>(() => FeedReader.Read(Fixtures.PathTo("members_E90002_bad_trailer.txt")));
        Assert.Contains("trailer", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("QQ2", error.Message, StringComparison.Ordinal);
    }
}
