using System.Text.Json;

namespace MemberIngestion.Members;

/// <summary>Where loaded members go. One member per member reference; a later load replaces it.</summary>
public interface IMemberStore
{
    void Upsert(Member member);

    Member? Find(string memberRef);

    /// <summary>Every member, in the order each was first loaded.</summary>
    IReadOnlyList<Member> All();
}

/// <summary>Keeps members in memory. For tests and dry runs.</summary>
public sealed class InMemoryMemberStore : IMemberStore
{
    private readonly List<Member> members = [];

    public void Upsert(Member member)
    {
        var index = members.FindIndex(m => m.MemberRef == member.MemberRef);
        if (index < 0)
        {
            members.Add(member);
        }
        else
        {
            members[index] = member;
        }
    }

    public Member? Find(string memberRef) => members.Find(m => m.MemberRef == memberRef);

    public IReadOnlyList<Member> All() => [.. members];
}

/// <summary>Keeps members in a JSON file. For local runs; production loads through the member service.</summary>
public sealed class JsonFileMemberStore(string path) : IMemberStore
{
    private readonly InMemoryMemberStore inner = Load(path);

    public void Upsert(Member member)
    {
        inner.Upsert(member);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(inner.All()));
    }

    public Member? Find(string memberRef) => inner.Find(memberRef);

    public IReadOnlyList<Member> All() => inner.All();

    private static InMemoryMemberStore Load(string path)
    {
        var store = new InMemoryMemberStore();
        if (File.Exists(path))
        {
            foreach (var member in JsonSerializer.Deserialize<List<Member>>(File.ReadAllText(path)) ?? [])
            {
                store.Upsert(member);
            }
        }
        return store;
    }
}
