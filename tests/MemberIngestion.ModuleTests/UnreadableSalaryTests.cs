using Scheme.Common.Feeds;

namespace MemberIngestion.ModuleTests;

public sealed class UnreadableSalaryTests : IDisposable
{
    private readonly IngestionHarness harness = new();
    private readonly string path = Path.Combine(Path.GetTempPath(), $"SYNTH-{Guid.NewGuid():N}.txt");

    public void Dispose()
    {
        harness.Dispose();
        File.Delete(path);
    }

    [Fact]
    public void A_file_with_an_unreadable_salary_loads_nothing()
    {
        File.WriteAllLines(path,
        [
            "HDR|MEMBERS|E90001|SYNTH-M-BAD|09/11/2026|3.1",
            "DTL|E90001|M900001|QQ100001A|Synthetic|Alex|14/03/1981|01/09/2015|41250.00",
            "DTL|E90001|M900002|QQ100002B|Example|Robin|28/11/1975|15/01/2003|41,250.00",
            "DTL|E90001|M900003|QQ100003C|Sample|Morgan|05/07/1990|01/04/2019|33800.00",
            "TRL|3",
        ]);
        Assert.Throws<FeedFormatException>(() => harness.Ingestor.IngestFile(path));
        Assert.Empty(harness.Store.All());
        Assert.Empty(harness.Audit.Events);
    }
}
