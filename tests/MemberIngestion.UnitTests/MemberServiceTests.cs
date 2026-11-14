using MemberIngestion.Members;

namespace MemberIngestion.UnitTests;

public sealed class MemberServiceTests
{
    private readonly InMemoryMemberStore store = new();
    private readonly MemberService service;

    public MemberServiceTests()
    {
        store.Upsert(Member.FromRecord(MemberRecords.Make(memberRef: "M900001"), "SYNTH-1"));
        store.Upsert(Member.FromRecord(MemberRecords.Make(memberRef: "M900002"), "SYNTH-1"));
        service = new MemberService(store);
    }

    [Fact]
    public void Find_returns_the_member_with_that_reference() =>
        Assert.Equal("M900002", service.Find("M900002")!.MemberRef);

    [Fact]
    public void Everyone_returns_every_member_in_the_store() =>
        Assert.Equal(["M900001", "M900002"], service.Everyone().Select(m => m.MemberRef));

    [Fact]
    public void How_many_counts_the_members_in_the_store() =>
        Assert.Equal(2, service.HowMany());

    [Fact]
    public void Exists_is_false_for_an_unknown_reference() =>
        Assert.False(service.Exists("M900999"));
}
