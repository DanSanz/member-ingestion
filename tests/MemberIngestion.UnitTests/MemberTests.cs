using MemberIngestion.Members;

namespace MemberIngestion.UnitTests;

public sealed class MemberTests
{
    [Theory]
    [InlineData("en-GB")]
    [InlineData("de-DE")]
    public void Salary_is_read_with_a_decimal_point_whatever_the_machine_culture(string culture)
    {
        using var _ = new CultureScope(culture);
        var member = Member.FromRecord(MemberRecords.Make(salary: "52300.50"), "SYNTH-M-E90001-20261102");
        Assert.Equal(52300.50m, member.Salary);
    }

    [Fact]
    public void Dates_stay_as_received()
    {
        var member = Member.FromRecord(MemberRecords.Make(dateOfBirth: "14/03/1981"), "SYNTH-M-E90001-20261102");
        Assert.Equal(("14/03/1981", "01/09/2015"), (member.DateOfBirth, member.DateJoined));
    }

    [Fact]
    public void Store_keeps_one_member_per_reference_in_first_load_order()
    {
        var store = new InMemoryMemberStore();
        store.Upsert(Member.FromRecord(MemberRecords.Make(memberRef: "M900001", salary: "100.00"), "SYNTH-1"));
        store.Upsert(Member.FromRecord(MemberRecords.Make(memberRef: "M900002"), "SYNTH-1"));
        store.Upsert(Member.FromRecord(MemberRecords.Make(memberRef: "M900001", salary: "200.00"), "SYNTH-2"));
        Assert.Equal(["M900001", "M900002"], store.All().Select(m => m.MemberRef));
        Assert.Equal(200.00m, store.Find("M900001")!.Salary);
    }
}
