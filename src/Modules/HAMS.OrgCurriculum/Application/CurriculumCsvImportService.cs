using System.Text;
using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Application;

internal sealed class CurriculumCsvImportService(OrgDbContext dbContext) : ICurriculumCsvImportService
{
    private const int ExpectedColumnCount = 8;

    public async Task<CurriculumCsvImportResult> ImportAsync(Guid syllabusId, Stream csvStream, CancellationToken cancellationToken = default)
    {
        var syllabus = await dbContext.Syllabuses.FindAsync([syllabusId], cancellationToken)
            ?? throw new InvalidOperationException("Syllabus not found.");

        if (syllabus.Status != RecordStatus.Draft)
        {
            throw new InvalidOperationException("Curriculum content can only be imported into a Draft syllabus.");
        }

        var strandsByCode = await dbContext.Strands
            .Where(s => s.SyllabusId == syllabusId)
            .ToDictionaryAsync(s => s.Code, cancellationToken);

        var subStrandsByCode = await dbContext.SubStrands
            .Where(ss => strandsByCode.Values.Select(s => s.Id).Contains(ss.StrandId))
            .ToDictionaryAsync(ss => (ss.StrandId, ss.Code), cancellationToken);

        var outcomesByCode = await dbContext.LearningOutcomes
            .Where(o => subStrandsByCode.Values.Select(ss => ss.Id).Contains(o.SubStrandId))
            .ToDictionaryAsync(o => (o.SubStrandId, o.Code), cancellationToken);

        int strandsCreated = 0, subStrandsCreated = 0, outcomesCreated = 0, indicatorsCreated = 0;

        using var reader = new StreamReader(csvStream, Encoding.UTF8, leaveOpen: true);

        var header = await reader.ReadLineAsync(cancellationToken);
        if (header is null)
        {
            throw new InvalidOperationException("CSV file is empty.");
        }

        string? line;
        var lineNumber = 1;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line);
            if (fields.Count != ExpectedColumnCount)
            {
                throw new InvalidOperationException(
                    $"Line {lineNumber}: expected {ExpectedColumnCount} columns, found {fields.Count}.");
            }

            var (strandCode, strandName, subStrandCode, subStrandName, outcomeCode, outcomeDescription, indicatorCode, indicatorDescription) =
                (fields[0], fields[1], fields[2], fields[3], fields[4], fields[5], fields[6], fields[7]);

            if (!strandsByCode.TryGetValue(strandCode, out var strand))
            {
                strand = new Strand { Id = Guid.NewGuid(), SyllabusId = syllabusId, Code = strandCode, Name = strandName, DisplayOrder = strandsByCode.Count + 1 };
                dbContext.Strands.Add(strand);
                strandsByCode[strandCode] = strand;
                strandsCreated++;
            }

            if (!subStrandsByCode.TryGetValue((strand.Id, subStrandCode), out var subStrand))
            {
                subStrand = new SubStrand { Id = Guid.NewGuid(), StrandId = strand.Id, Code = subStrandCode, Name = subStrandName, DisplayOrder = subStrandsByCode.Count + 1 };
                dbContext.SubStrands.Add(subStrand);
                subStrandsByCode[(strand.Id, subStrandCode)] = subStrand;
                subStrandsCreated++;
            }

            if (!outcomesByCode.TryGetValue((subStrand.Id, outcomeCode), out var outcome))
            {
                outcome = new LearningOutcome { Id = Guid.NewGuid(), SubStrandId = subStrand.Id, Code = outcomeCode, Description = outcomeDescription, DisplayOrder = outcomesByCode.Count + 1 };
                dbContext.LearningOutcomes.Add(outcome);
                outcomesByCode[(subStrand.Id, outcomeCode)] = outcome;
                outcomesCreated++;
            }

            dbContext.Indicators.Add(new Indicator
            {
                Id = Guid.NewGuid(), LearningOutcomeId = outcome.Id, Code = indicatorCode, Description = indicatorDescription, DisplayOrder = indicatorsCreated + 1,
            });
            indicatorsCreated++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CurriculumCsvImportResult(strandsCreated, subStrandsCreated, outcomesCreated, indicatorsCreated);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
