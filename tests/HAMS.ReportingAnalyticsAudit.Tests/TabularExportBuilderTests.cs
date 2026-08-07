using System.Text;
using ClosedXML.Excel;
using HAMS.ReportingAnalyticsAudit.Application;

namespace HAMS.ReportingAnalyticsAudit.Tests;

public class TabularExportBuilderTests
{
    [Fact]
    public void BuildCsv_writes_a_header_row_and_one_row_per_record()
    {
        string[] headers = ["Name", "Grade"];
        string[][] rows = [["Aisha", "Grade 5"], ["Hassan", "Grade 6"]];

        var csv = Encoding.UTF8.GetString(TabularExportBuilder.BuildCsv(headers, rows));

        var lines = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("Name,Grade", lines[0]);
        Assert.Equal("Aisha,Grade 5", lines[1]);
        Assert.Equal("Hassan,Grade 6", lines[2]);
    }

    [Fact]
    public void BuildCsv_quotes_fields_containing_commas()
    {
        var csv = Encoding.UTF8.GetString(TabularExportBuilder.BuildCsv(["Name"], [["Doe, Jane"]]));

        Assert.Contains("\"Doe, Jane\"", csv);
    }

    [Fact]
    public void BuildCsv_quotes_and_escapes_fields_containing_double_quotes()
    {
        var csv = Encoding.UTF8.GetString(TabularExportBuilder.BuildCsv(["Notes"], [["She said \"hello\""]]));

        Assert.Contains("\"She said \"\"hello\"\"\"", csv);
    }

    [Fact]
    public void BuildCsv_quotes_fields_containing_newlines()
    {
        var csv = Encoding.UTF8.GetString(TabularExportBuilder.BuildCsv(["Notes"], [["Line one\nLine two"]]));

        Assert.Contains("\"Line one\nLine two\"", csv);
    }

    [Fact]
    public void BuildCsv_leaves_plain_fields_unquoted()
    {
        var csv = Encoding.UTF8.GetString(TabularExportBuilder.BuildCsv(["Name"], [["Plain Value"]]));

        Assert.DoesNotContain('"', csv);
    }

    [Fact]
    public void BuildXlsx_produces_a_real_workbook_with_the_header_row_and_data_rows()
    {
        string[] headers = ["Name", "Grade"];
        string[][] rows = [["Aisha", "Grade 5"], ["Hassan", "Grade 6"]];

        var bytes = TabularExportBuilder.BuildXlsx("Roster", headers, rows);

        Assert.NotEmpty(bytes);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Roster");

        Assert.Equal("Name", sheet.Cell(1, 1).GetString());
        Assert.Equal("Grade", sheet.Cell(1, 2).GetString());
        Assert.Equal("Aisha", sheet.Cell(2, 1).GetString());
        Assert.Equal("Grade 5", sheet.Cell(2, 2).GetString());
        Assert.Equal("Hassan", sheet.Cell(3, 1).GetString());
        Assert.True(sheet.Row(1).Style.Font.Bold);
    }

    [Fact]
    public void BuildXlsx_handles_an_empty_row_set_without_throwing()
    {
        var bytes = TabularExportBuilder.BuildXlsx("Empty", ["Name"], []);

        Assert.NotEmpty(bytes);
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        Assert.Equal("Name", workbook.Worksheet("Empty").Cell(1, 1).GetString());
    }
}
