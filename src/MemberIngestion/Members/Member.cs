using System.Globalization;
using MemberIngestion.Generated;

namespace MemberIngestion.Members;

/// <summary>
/// A member as the member database holds it. Dates stay as received (DD/MM/YYYY text): the database
/// stores them as text, and validating them is not the store's job.
/// </summary>
public sealed record Member(
    string MemberRef,
    string EmployerId,
    string NiNumber,
    string Surname,
    string Forenames,
    string DateOfBirth,
    string DateJoined,
    decimal Salary,
    string SourceFileId)
{
    public static Member FromRecord(MemberRecord record, string sourceFileId) => new(
        record.MemberRef,
        record.EmployerId,
        record.NiNumber,
        record.Surname,
        record.Forenames,
        record.DateOfBirth,
        record.DateJoined,
        decimal.Parse(record.Salary, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture),
        sourceFileId);
}
