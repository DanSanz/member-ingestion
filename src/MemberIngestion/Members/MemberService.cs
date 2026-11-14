namespace MemberIngestion.Members;

/// <summary>Member lookups for the reports.</summary>
public sealed class MemberService(IMemberStore store)
{
    public Member? Find(string memberId) => store.Find(memberId);

    public IReadOnlyList<Member> Everyone() => store.All();

    public int HowMany() => store.All().Count;

    public bool Exists(string memberId) => store.Find(memberId) is not null;
}
