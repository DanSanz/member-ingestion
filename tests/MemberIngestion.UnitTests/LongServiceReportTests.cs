using System.Globalization;
using MemberIngestion.Members;
using MemberIngestion.Reports;

namespace MemberIngestion.UnitTests;

public sealed class LongServiceReportTests
{
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    private static Member Joined(string memberRef, string dateJoined) =>
        Member.FromRecord(MemberRecords.Make(memberRef: memberRef, dateJoined: dateJoined), "SYNTH-1");

    [Fact]
    public void Lists_a_member_on_the_day_they_reach_25_years()
    {
        var member = Joined("M900301", "02/11/2001");
        var asAt = DateOnly.Parse(member.DateJoined, Uk).AddYears(25);
        var lines = LongServiceReport.For([member], "E90001", asAt).Split(Environment.NewLine);
        Assert.Contains("M900301 25 years", lines);
    }

    [Fact]
    public void Leaves_out_a_member_one_day_short_of_25_years()
    {
        var member = Joined("M900302", "02/11/2001");
        var asAt = DateOnly.Parse(member.DateJoined, Uk).AddYears(25).AddDays(-1);
        var report = LongServiceReport.For([member], "E90001", asAt);
        Assert.DoesNotContain("M900302", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Counts_whole_years_only()
    {
        var member = Joined("M900303", "01/04/1990");
        var asAt = DateOnly.Parse(member.DateJoined, Uk).AddYears(36).AddDays(-1);
        var lines = LongServiceReport.For([member], "E90001", asAt).Split(Environment.NewLine);
        Assert.Contains("M900303 35 years", lines);
    }
}
