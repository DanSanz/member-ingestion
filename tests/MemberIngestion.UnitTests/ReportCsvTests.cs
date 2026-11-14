using MemberIngestion.Reports;

namespace MemberIngestion.UnitTests;

public sealed class ReportCsvTests
{
    [Fact]
    public void Plain_cells_are_joined_with_commas() =>
        Assert.Equal("M900001,01/11/2026", ReportCsv.Line("M900001", "01/11/2026"));

    [Fact]
    public void A_cell_with_a_comma_is_quoted() =>
        Assert.Equal("\"Synthetic Supplies, North\",E90031", ReportCsv.Line("Synthetic Supplies, North", "E90031"));

    [Fact]
    public void A_quote_inside_a_cell_is_doubled() =>
        Assert.Equal("\"The \"\"Example\"\" Group\"", ReportCsv.Line("The \"Example\" Group"));

    [Fact]
    public void A_table_has_the_header_row_first()
    {
        var rows = ReportCsv.Table(["Member number", "Start date"], [["M900001", "01/11/2026"]]).Split("\r\n");
        Assert.Equal("Member number,Start date", rows[0]);
        Assert.Equal("M900001,01/11/2026", rows[1]);
    }
}
