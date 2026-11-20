using MemberIngestion.Feeds;
using MemberIngestion.Generated;
using MemberIngestion.Ingestion;
using Microsoft.Extensions.Options;

namespace MemberIngestion.UnitTests;

public sealed class SenderCheckTests
{
    [Fact]
    public void A_file_with_only_the_senders_rows_gives_an_empty_list()
    {
        MemberRecord[] rows =
        [
            MemberRecords.Make(employerId: "E90001", memberRef: "M900001"),
            MemberRecords.Make(employerId: "E90001", memberRef: "M900002"),
            MemberRecords.Make(employerId: "E90001", memberRef: "M900003"),
        ];
        Assert.Empty(SenderCheck.RowsForOtherEmployers("E90001", rows));
    }

    [Fact]
    public void A_row_for_another_employer_is_returned_by_member_reference()
    {
        MemberRecord[] rows =
        [
            MemberRecords.Make(employerId: "E90001", memberRef: "M900001"),
            MemberRecords.Make(employerId: "E90002", memberRef: "M900002"),
        ];
        Assert.Equal(["M900002"], SenderCheck.RowsForOtherEmployers("E90001", rows));
    }

    [Fact]
    public void Rows_are_returned_in_file_order()
    {
        MemberRecord[] rows =
        [
            MemberRecords.Make(employerId: "E90003", memberRef: "M900009"),
            MemberRecords.Make(employerId: "E90001", memberRef: "M900001"),
            MemberRecords.Make(employerId: "E90002", memberRef: "M900004"),
        ];
        Assert.Equal(["M900009", "M900004"], SenderCheck.RowsForOtherEmployers("E90001", rows));
    }

    [Fact]
    public void The_mixed_fixture_has_one_row_for_another_employer()
    {
        var reader = new MemberFeedReader(Options.Create(new IngestionOptions()));
        var file = reader.Read(Fixtures.PathTo("members_E90012_20261116_mixed.txt"));
        Assert.Equal(["M901301"], SenderCheck.RowsForOtherEmployers(file.Header.SenderId, file.Rows));
    }
}
