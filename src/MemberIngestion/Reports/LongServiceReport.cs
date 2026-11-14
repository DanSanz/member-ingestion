using System.Globalization;
using MemberIngestion.Members;

namespace MemberIngestion.Reports;

/// <summary>
/// The members of one employer with 25 years or more of service. The length of service runs from the
/// joining date to the date the report is run for.
/// </summary>
public static class LongServiceReport
{
    private const int Threshold = 25;

    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    public static string For(IEnumerable<Member> members, string employerId, DateOnly asAt)
    {
        var lines = new List<string> { $"Long service at {employerId}" };
        var longServing = members
            .Where(m => m.EmployerId == employerId && Qualifies(m, asAt))
            .OrderBy(m => DateOnly.Parse(m.DateJoined, Uk));
        foreach (var m in longServing)
        {
            var serviceStart = DateOnly.Parse(m.DateJoined, Uk);
            var tenureYears = WholeYears(serviceStart, asAt);
            lines.Add($"{m.MemberRef} {tenureYears} years");
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static bool Qualifies(Member member, DateOnly asAt) =>
        WholeYears(DateOnly.FromDateTime(DateTime.Parse(member.DateJoined, Uk)), asAt) >= Threshold;

    private static int WholeYears(DateOnly from, DateOnly to)
    {
        var years = to.Year - from.Year;
        return from.AddYears(years) > to ? years - 1 : years;
    }
}
