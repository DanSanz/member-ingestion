using MemberIngestion.Members;
using Scheme.Common.Dates;

namespace MemberIngestion.Reports;

/// <summary>
/// How many members of one employer fall in each age band. A member whose date of birth cannot be
/// read is left out. The report gives counts only. An age is in whole years on the date the report is
/// run for.
/// </summary>
public static class AgeProfileReport
{
    private static readonly (string Name, int From, int To)[] Bands =
    [
        ("under 25", 0, 24),
        ("25-34", 25, 34),
        ("35-44", 35, 44),
        ("45-54", 45, 54),
        ("55-64", 55, 64),
        ("65+", 65, int.MaxValue),
    ];

    public static string For(IEnumerable<Member> members, string employerId, DateOnly asAt)
    {
        var ages = new List<int>();
        foreach (var m in members.Where(m => m.EmployerId == employerId))
        {
            if (UkDate.TryParse(m.DateOfBirth, out var born))
            {
                ages.Add(WholeYears(born, asAt));
            }
        }

        var lines = new List<string> { $"Age profile for {employerId}" };
        lines.AddRange(Bands.Select(band => $"{band.Name}: {ages.Count(age => age >= band.From && age <= band.To)}"));
        return string.Join(Environment.NewLine, lines);
    }

    public static int AverageAge(IEnumerable<Member> members, string employerId, DateOnly asAt)
    {
        var ages = new List<int>();
        foreach (var m in members.Where(m => m.EmployerId == employerId))
        {
            if (UkDate.TryParse(m.DateOfBirth, out var bornOn))
            {
                ages.Add(WholeYears(bornOn, asAt));
            }
        }
        return ages.Count == 0 ? 0 : (int)ages.Average();
    }

    private static int WholeYears(DateOnly from, DateOnly to)
    {
        var years = to.Year - from.Year;
        return from.AddYears(years) > to ? years - 1 : years;
    }
}
