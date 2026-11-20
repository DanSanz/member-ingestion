using MemberIngestion.Generated;

namespace MemberIngestion.Ingestion;

/// <summary>Finds the rows of a member file that belong to an employer other than its sender.</summary>
public static class SenderCheck
{
    /// <summary>The member references of the rows whose employer is not <paramref name="senderId"/>, in file order.</summary>
    public static IReadOnlyList<string> RowsForOtherEmployers(string senderId, IEnumerable<MemberRecord> rows) =>
        rows.Where(row => row.EmployerId != senderId).Select(row => row.MemberRef).ToList();
}
