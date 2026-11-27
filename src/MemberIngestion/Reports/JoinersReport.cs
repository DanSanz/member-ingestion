using MemberIngestion.Members;
using Scheme.Common.Dates;

namespace MemberIngestion.Reports;

/// <summary>
/// The members who joined an organisation in the month of its file, so Employer Relations can send
/// each one a welcome pack. Member references only, never names.
/// </summary>
public static class JoinersReport
{
    public static string For(IEnumerable<Member> members, string employerId, DateOnly fileDate)
    {
        var lines = new List<string> { $"New starters for {employerId}" };
        lines.AddRange(Joiners(members, employerId, fileDate).Select(m => $"{m.MemberRef} start date {m.DateJoined}"));
        return string.Join(Environment.NewLine, lines);
    }

    public static string Csv(IEnumerable<Member> members, string employerId, DateOnly fileDate) =>
        ReportCsv.Table(
            ["Member number", "Start date"],
            Joiners(members, employerId, fileDate).Select(m => new[] { m.MemberRef, m.DateJoined }));

    private static IEnumerable<Member> Joiners(IEnumerable<Member> members, string employerId, DateOnly fileDate) =>
        members
            .Where(m => m.EmployerId == employerId)
            .Where(m => UkDate.Parse(m.DateJoined).Year == fileDate.Year
                && UkDate.Parse(m.DateJoined).Month == fileDate.Month)
            .OrderBy(m => UkDate.Parse(m.DateJoined));
}
