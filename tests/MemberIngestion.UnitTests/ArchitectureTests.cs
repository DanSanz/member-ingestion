using MemberIngestion.Members;
using NetArchTest.Rules;
namespace MemberIngestion.UnitTests;

public sealed class ArchitectureTests
{
    const string N = "MemberIngestion.";
    // Known breaks, each with a reason.
    // Form: "Type -> Namespace".
    static readonly string[] Known = [
        // MemberFeedReader reads the
        // ingestion options to get
        // StrictSchema. That is a
        // known problem, to fix in its
        // own change.
        "MemberIngestion.Feeds.MemberFeedReader -> Ingestion",
    ];
    [Theory]
    [InlineData("Feeds", "Ingestion")]
    [InlineData("Feeds", "Reports")]
    [InlineData("Ingestion", "Reports")]
    [InlineData("Members", "Ingestion")]
    [InlineData("Members", "Reports")]
    [InlineData("Members", "Feeds")]
    public void Points_down(
        string from, string to)
    {
        var r = Types.InAssembly(
                typeof(Member).Assembly)
            .That()
            .ResideInNamespace(N + from)
            .ShouldNot()
            .HaveDependencyOn(N + to)
            .GetResult();
        var broken =
            (r.FailingTypeNames ?? [])
            .Where(t => !Known.Contains(
                $"{t} -> {to}"));
        Assert.Empty(broken);
    }
}
