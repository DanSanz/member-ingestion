// Acceptance tests: DOB quarantine spec v2, task T1 (criteria 1 and 2, and the constraints).
// Written by the team before T1 starts. They do not build until T1 is done; tests for T2-T5 are
// added with those tasks. All data here is synthetic.

using MemberIngestion.Quarantine;

namespace MemberIngestion.AcceptanceTests;

public sealed class DobQuarantineTests : IDisposable
{
    private static readonly DateOnly FileDate = new(2026, 11, 2);

    private readonly IngestionHarness harness = new();

    private DobQuarantine Quarantine => harness.Get<DobQuarantine>();

    public void Dispose() => harness.Dispose();

    // Criterion 1: an unparseable DOB is quarantined with DOB-01.

    [Fact]
    public void Unparseable_dob_is_quarantined_with_DOB_01()
    {
        var row = MemberRecords.Make(memberRef: "M900001", dateOfBirth: "31/02/1980");
        var result = Quarantine.ValidateFile([row], FileDate);
        Assert.Empty(result.Accepted);
        var held = Assert.Single(result.Quarantined);
        Assert.Same(row, held.Row);
        Assert.Equal("DOB-01", held.RuleId);
    }

    [Theory]
    [InlineData("29/02/1981")] // not a leap year
    [InlineData("03/14/1981")] // month first
    [InlineData("1981-03-14")] // ISO
    [InlineData("14/03/81")] // two-digit year
    [InlineData("")] // empty
    public void Dob_that_is_not_a_real_dd_mm_yyyy_date_is_DOB_01(string raw)
    {
        var result = Quarantine.ValidateFile([MemberRecords.Make(dateOfBirth: raw)], FileDate);
        Assert.Equal(["DOB-01"], result.Quarantined.Select(q => q.RuleId));
    }

    // Criterion 2: a valid row continues unchanged, on a scheme laptop (en-GB) and on a build agent (en-US).

    [Theory]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    public void Valid_row_continues_unchanged(string machineCulture)
    {
        using var _ = new CultureScope(machineCulture);
        var row = MemberRecords.Make(dateOfBirth: "14/03/1981"); // day over 12: wrong if read month first
        var result = Quarantine.ValidateFile([row], FileDate);
        Assert.Empty(result.Quarantined);
        Assert.Same(row, Assert.Single(result.Accepted));
    }

    [Fact]
    public void Leap_day_in_a_leap_year_is_valid()
    {
        var result = Quarantine.ValidateFile([MemberRecords.Make(dateOfBirth: "29/02/1988")], FileDate);
        Assert.Empty(result.Quarantined);
    }

    [Fact]
    public void Valid_rows_continue_in_order_around_a_quarantined_row()
    {
        var result = Quarantine.ValidateFile(
            [
                MemberRecords.Make(memberRef: "M900001", dateOfBirth: "14/03/1981"),
                MemberRecords.Make(memberRef: "M900002", dateOfBirth: "31/02/1980"),
                MemberRecords.Make(memberRef: "M900003", dateOfBirth: "23/08/1979"),
            ],
            FileDate);
        Assert.Equal(["M900001", "M900003"], result.Accepted.Select(row => row.MemberRef));
        Assert.Equal(["M900002"], result.Quarantined.Select(q => q.Row.MemberRef));
    }

    // Constraints: every quarantine is audited with actor and rule ID; no personal data leaks.

    [Fact]
    public void Quarantine_is_audited_with_actor_and_rule_id()
    {
        Quarantine.ValidateFile([MemberRecords.Make(memberRef: "M900001", dateOfBirth: "31/02/1980")], FileDate);
        var held = Assert.Single(harness.Audit.OfType("dob_quarantined"));
        Assert.Equal("M900001", held.SubjectRef);
        Assert.Equal("DOB-01", held.RuleId);
        Assert.Equal(harness.Options.AuditActor, held.Actor);
    }

    [Fact]
    public void No_dob_or_ni_number_in_logs_or_audit()
    {
        var row = MemberRecords.Make(memberRef: "M900001", niNumber: "QQ123456C", dateOfBirth: "31/02/1980");
        Quarantine.ValidateFile([row], FileDate);
        var recorded = harness.Logs.Text + string.Join(" ", harness.Audit.Events.Select(e => $"{e.SubjectRef} {e.Detail}"));
        Assert.DoesNotContain("31/02/1980", recorded, StringComparison.Ordinal);
        Assert.DoesNotContain("QQ123456C", recorded, StringComparison.Ordinal);
    }

    // Tracer: the nightly member load holds the row back and loads the rest.

    [Fact]
    public void Ingest_loads_valid_members_and_holds_back_DOB_01()
    {
        harness.Ingestor.IngestFile(Fixtures.PathTo("members_E90004_20261102_dob.txt"));
        Assert.Equal(["M940001", "M940003"], harness.Store.All().Select(member => member.MemberRef));
        Assert.Equal(["M940002"], harness.Audit.OfType("dob_quarantined").Select(e => e.SubjectRef));
    }
}
