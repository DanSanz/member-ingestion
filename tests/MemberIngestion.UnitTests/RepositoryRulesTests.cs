using System.Text.RegularExpressions;
using FeedTypes;

namespace MemberIngestion.UnitTests;

public sealed class GeneratedCodeTests
{
    [Fact]
    public void Generated_files_match_the_contract()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "contract");
        var expected = FeedTypeGenerator.Generate(File.ReadAllText(Path.Combine(folder, "member-feed.json")));
        var checkedIn = File.ReadAllText(Path.Combine(folder, "MemberRecord.g.cs.txt"));
        Assert.True(
            expected.ReplaceLineEndings("\n") == checkedIn.ReplaceLineEndings("\n"),
            $"{FeedTypeGenerator.OutputPath} does not match {FeedTypeGenerator.ContractPath}. Generated files are " +
            "never edited by hand: change the contract (CODEOWNERS), then run `dotnet run --project tools/FeedTypes`.");
    }
}

public sealed partial class FixturesAreSyntheticTests
{
    public static TheoryData<string> FixtureFiles =>
        new(Directory.GetFiles(Fixtures.Folder, "*.txt").Select(path => Path.GetFileName(path)));

    [Theory]
    [MemberData(nameof(FixtureFiles))]
    public void File_id_is_marked_synthetic(string fileName) =>
        Assert.StartsWith("SYNTH-", Header(fileName)[3], StringComparison.Ordinal);

    [Theory]
    [MemberData(nameof(FixtureFiles))]
    public void Employer_is_in_the_test_range(string fileName) =>
        Assert.Matches(TestEmployer(), Header(fileName)[2]);

    [Theory]
    [MemberData(nameof(FixtureFiles))]
    public void Ni_numbers_use_the_unissued_prefix(string fileName)
    {
        if (Header(fileName)[1] != "MEMBERS")
        {
            return;
        }
        foreach (var line in File.ReadLines(Fixtures.PathTo(fileName)).Where(l => l.StartsWith("DTL|", StringComparison.Ordinal)))
        {
            Assert.StartsWith("QQ", line.Split('|')[3], StringComparison.Ordinal);
        }
    }

    private static string[] Header(string fileName) => File.ReadLines(Fixtures.PathTo(fileName)).First().Split('|');

    [GeneratedRegex("^E9[0-9]{4}$")]
    private static partial Regex TestEmployer();
}
