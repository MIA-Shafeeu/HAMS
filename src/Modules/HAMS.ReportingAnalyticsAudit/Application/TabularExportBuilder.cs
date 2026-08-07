using System.Text;
using ClosedXML.Excel;

namespace HAMS.ReportingAnalyticsAudit.Application;

/// <summary>
/// One shared row/column shape (plain display strings) feeding both export formats, so every report
/// method only needs to describe ITS data once — CSV needs no library (the format is simple enough
/// to hand-roll correctly with proper quoting, same judgment call as everywhere else in this
/// codebase that avoids a dependency for something this small); XLSX uses ClosedXML (Phase 11's
/// PdfSharpCore-over-QuestPDF precedent: MIT-licensed, no revenue-conditional terms).
/// </summary>
internal static class TabularExportBuilder
{
    public static byte[] BuildCsv(IReadOnlyList<string> headers, IReadOnlyList<string[]> rows)
    {
        var builder = new StringBuilder();
        builder.Append(string.Join(',', headers.Select(EscapeCsvField))).Append("\r\n");

        foreach (var row in rows)
        {
            builder.Append(string.Join(',', row.Select(EscapeCsvField))).Append("\r\n");
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public static byte[] BuildXlsx(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<string[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        for (var col = 0; col < headers.Count; col++)
        {
            sheet.Cell(1, col + 1).Value = headers[col];
        }

        sheet.Row(1).Style.Font.Bold = true;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var col = 0; col < row.Length; col++)
            {
                sheet.Cell(rowIndex + 2, col + 1).Value = row[col];
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
