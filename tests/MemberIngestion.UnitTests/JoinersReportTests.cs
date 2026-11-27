using MemberIngestion.Members;
using MemberIngestion.Reports;
using Scheme.Common.Dates;

namespace MemberIngestion.UnitTests;

public sealed class JoinersReportTests
{
    private static Member Joined(string memberRef, string dateJoined, string employerId = "E90001") =>
        Member.FromRecord(MemberRecords.Make(employerId: employerId, memberRef: memberRef, dateJoined: dateJoined), "SYNTH-1");

    [Fact]
    public void Lists_a_member_who_joined_in_the_month_of_the_file()
    {
        var joiner = Joined("M900201", "01/11/2026");
        var fileDate = UkDate.Parse(joiner.DateJoined).AddDays(1);
        var lines = JoinersReport.For([joiner], "E90001", fileDate).Split(Environment.NewLine);
        Assert.Contains("M900201 start date 01/11/2026", lines);
    }

    [Fact]
    public void Leaves_out_a_member_who_joined_in_an_earlier_month()
    {
        // The file comes two days later, in the next month.
        var member = Joined("M900202", "30/09/2026");
        var fileDate = UkDate.Parse(member.DateJoined).AddDays(2);
        var report = JoinersReport.For([member], "E90001", fileDate);
        Assert.DoesNotContain("M900202", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Csv_has_one_row_per_joiner_of_that_employer()
    {
        var joiner = Joined("M900203", "01/09/2026");
        var members = new[] { joiner, Joined("M920203", "01/09/2026", employerId: "E90002") };
        var fileDate = UkDate.Parse(joiner.DateJoined).AddDays(1);
        var rows = JoinersReport.Csv(members, "E90001", fileDate).Split("\r\n");
        Assert.Contains("M900203,01/09/2026", rows);
        Assert.DoesNotContain("M920203,01/09/2026", rows);
    }
}
