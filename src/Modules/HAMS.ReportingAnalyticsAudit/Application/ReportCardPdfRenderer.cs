using HAMS.LearningDelivery.Application;
using HAMS.ReportingAnalyticsAudit.Domain;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace HAMS.ReportingAnalyticsAudit.Application;

/// <summary>
/// Renders a <see cref="ReportCard"/> to PDF bytes, fresh, from the stored structured data — never
/// itself a stored artifact (see <see cref="IReportCardService"/>'s remarks on why this phase needs
/// no file-storage pipeline at all). Deliberately a plain, low-level <c>XGraphics</c> layout rather
/// than a templating library — a report card's layout is simple enough (narrative, a results table,
/// a competency table, next steps) not to need one.
/// </summary>
internal static class ReportCardPdfRenderer
{
    private const double MarginPoints = 40;

    public static byte[] Render(
        ReportCard reportCard, IReadOnlyList<ReportCardSubjectResult> subjectResults, IReadOnlyDictionary<Guid, string> subjectNames,
        IReadOnlyList<ReportCardKeyCompetencySummary> competencySummaries, IReadOnlyDictionary<Guid, KeyCompetencyName> competencyNames)
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);

        var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
        var headingFont = new XFont("Arial", 13, XFontStyle.Bold);
        var bodyFont = new XFont("Arial", 11, XFontStyle.Regular);

        double y = MarginPoints;
        double width = page.Width.Point - (2 * MarginPoints);

        y = DrawLine(gfx, "Student Report Card", titleFont, MarginPoints, y) + 10;
        y = DrawLine(gfx, $"Version {reportCard.Version}  |  Status: {reportCard.ApprovalStatus}", bodyFont, MarginPoints, y) + 20;

        y = DrawLine(gfx, "Learning Progress", headingFont, MarginPoints, y) + 4;
        y = DrawWrapped(gfx, reportCard.NarrativeEn, bodyFont, MarginPoints, y, width) + 16;

        y = DrawLine(gfx, "Subject Results", headingFont, MarginPoints, y) + 6;
        foreach (var result in subjectResults)
        {
            var name = subjectNames.GetValueOrDefault(result.SubjectId, "(unknown subject)");
            var parts = new List<string>();
            if (result.Percentage is { } percentage) parts.Add($"{percentage:0.#}%");
            if (result.AchievementLevelId is not null) parts.Add("Level recorded");
            if (result.GradeBandId is not null) parts.Add("Grade band recorded");

            y = DrawLine(gfx, $"{name}: {string.Join(", ", parts)}", bodyFont, MarginPoints, y) + 2;
        }

        y += 14;
        y = DrawLine(gfx, "Key Competencies", headingFont, MarginPoints, y) + 6;
        foreach (var summary in competencySummaries)
        {
            var name = competencyNames.TryGetValue(summary.KeyCompetencyId, out var n) ? n.NameEn : "(unknown competency)";
            var rating = summary.AverageRatingScore is { } avg ? $", average rating {avg:0.#}" : string.Empty;
            y = DrawLine(gfx, $"{name}: {summary.EvidenceCount} piece(s) of evidence{rating}", bodyFont, MarginPoints, y) + 2;
        }

        y += 14;
        y = DrawLine(gfx, "Next Steps", headingFont, MarginPoints, y) + 4;
        DrawWrapped(gfx, reportCard.NextStepsEn, bodyFont, MarginPoints, y, width);

        using var stream = new MemoryStream();
        document.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static double DrawLine(XGraphics gfx, string text, XFont font, double x, double y)
    {
        gfx.DrawString(text, font, XBrushes.Black, new XPoint(x, y + font.Height));
        return y + font.Height + 4;
    }

    private static double DrawWrapped(XGraphics gfx, string text, XFont font, double x, double y, double maxWidth)
    {
        foreach (var line in WrapText(gfx, text, font, maxWidth))
        {
            y = DrawLine(gfx, line, font, x, y);
        }

        return y;
    }

    private static IEnumerable<string> WrapText(XGraphics gfx, string text, XFont font, double maxWidth)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = string.Empty;

        foreach (var word in words)
        {
            var candidate = line.Length == 0 ? word : $"{line} {word}";
            if (gfx.MeasureString(candidate, font).Width > maxWidth && line.Length > 0)
            {
                yield return line;
                line = word;
            }
            else
            {
                line = candidate;
            }
        }

        if (line.Length > 0)
        {
            yield return line;
        }
    }
}
