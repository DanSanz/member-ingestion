using MemberIngestion.Members;
using MemberIngestion.Reports;
using Scheme.Common.Dates;

namespace MemberIngestion.UnitTests;

public sealed class AgeProfileReportTests
{
    private static Member Born(string memberRef, string dateOfBirth) =>
        Member.FromRecord(MemberRecords.Make(memberRef: memberRef, dateOfBirth: dateOfBirth), "SYNTH-1");

    [Fact]
    public void A_member_on_their_25th_birthday_is_in_the_25_to_34_band()
    {
        var member = Born("M900401", "02/11/2001");
        var asAt = UkDate.Parse(member.DateOfBirth).AddYears(25);
        var lines = AgeProfileReport.For([member], "E90001", asAt).Split(Environment.NewLine);
        Assert.Contains("25-34: 1", lines);
    }

    [Fact]
    public void A_member_the_day_before_their_25th_birthday_is_in_the_under_25_band()
    {
        var member = Born("M900402", "02/11/2001");
        var asAt = UkDate.Parse(member.DateOfBirth).AddYears(25).AddDays(-1);
        var lines = AgeProfileReport.For([member], "E90001", asAt).Split(Environment.NewLine);
        Assert.Contains("under 25: 1", lines);
    }

    [Fact]
    public void Average_age_is_the_mean_in_whole_years()
    {
        var younger = Born("M900403", "02/11/1996");
        var members = new[] { younger, Born("M900404", "02/11/1986") };
        var asAt = UkDate.Parse(younger.DateOfBirth).AddYears(30);
        Assert.Equal(35, AgeProfileReport.AverageAge(members, "E90001", asAt));
    }

    [Fact]
    public void Average_age_is_zero_for_an_employer_with_no_members()
    {
        var member = Born("M900405", "02/11/1996");
        Assert.Equal(0, AgeProfileReport.AverageAge([member], "E90002", new DateOnly(2026, 11, 2)));
    }

    [Fact]
    public void An_average_part_way_through_a_year_is_rounded_down()
    {
        var younger = Born("M900406", "02/11/1996");
        var members = new[] { younger, Born("M900407", "02/11/1995") };
        var asAt = UkDate.Parse(younger.DateOfBirth).AddYears(30);
        Assert.Equal(30, AgeProfileReport.AverageAge(members, "E90001", asAt));
    }
}
