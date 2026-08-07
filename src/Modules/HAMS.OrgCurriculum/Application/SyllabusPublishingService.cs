using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using HAMS.Platform.Audit.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Application;

internal sealed class SyllabusPublishingService(OrgDbContext dbContext) : ISyllabusPublishingService
{
    public async Task<Guid> CreateInitialDraftAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        var syllabus = new Syllabus
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            Version = 1,
            // IsCurrent flips to true only in PublishAsync — see its remarks, and KeyStagePolicy's
            // identical rule — never at Draft creation, so a lineage never has two current rows.
            IsCurrent = false,
            Status = RecordStatus.Draft,
        };

        dbContext.Syllabuses.Add(syllabus);
        await dbContext.SaveChangesAsync(cancellationToken);

        return syllabus.Id;
    }

    public async Task<Guid> CreateDraftRevisionAsync(Guid existingSyllabusId, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Syllabuses.FindAsync([existingSyllabusId], cancellationToken)
            ?? throw new InvalidOperationException("Syllabus not found.");

        var strands = await dbContext.Strands
            .Where(s => s.SyllabusId == existingSyllabusId)
            .ToListAsync(cancellationToken);
        var strandIds = strands.Select(s => s.Id).ToList();

        var subStrands = await dbContext.SubStrands
            .Where(ss => strandIds.Contains(ss.StrandId))
            .ToListAsync(cancellationToken);
        var subStrandIds = subStrands.Select(ss => ss.Id).ToList();

        var outcomes = await dbContext.LearningOutcomes
            .Where(o => subStrandIds.Contains(o.SubStrandId))
            .ToListAsync(cancellationToken);
        var outcomeIds = outcomes.Select(o => o.Id).ToList();

        var indicators = await dbContext.Indicators
            .Where(i => outcomeIds.Contains(i.LearningOutcomeId))
            .ToListAsync(cancellationToken);

        var prerequisites = await dbContext.LearningOutcomePrerequisites
            .Where(p => outcomeIds.Contains(p.LearningOutcomeId))
            .ToListAsync(cancellationToken);

        var newSyllabus = new Syllabus
        {
            Id = Guid.NewGuid(),
            SubjectId = existing.SubjectId,
            Version = existing.Version + 1,
            IsCurrent = false,
            SupersedesId = existing.Id,
            Status = RecordStatus.Draft,
        };
        dbContext.Syllabuses.Add(newSyllabus);

        var strandIdMap = new Dictionary<Guid, Guid>();
        foreach (var strand in strands)
        {
            var newId = Guid.NewGuid();
            strandIdMap[strand.Id] = newId;
            dbContext.Strands.Add(new Strand
            {
                Id = newId, SyllabusId = newSyllabus.Id, Code = strand.Code, Name = strand.Name, DisplayOrder = strand.DisplayOrder,
            });
        }

        var subStrandIdMap = new Dictionary<Guid, Guid>();
        foreach (var subStrand in subStrands)
        {
            var newId = Guid.NewGuid();
            subStrandIdMap[subStrand.Id] = newId;
            dbContext.SubStrands.Add(new SubStrand
            {
                Id = newId, StrandId = strandIdMap[subStrand.StrandId], Code = subStrand.Code, Name = subStrand.Name, DisplayOrder = subStrand.DisplayOrder,
            });
        }

        var outcomeIdMap = new Dictionary<Guid, Guid>();
        foreach (var outcome in outcomes)
        {
            var newId = Guid.NewGuid();
            outcomeIdMap[outcome.Id] = newId;
            dbContext.LearningOutcomes.Add(new LearningOutcome
            {
                Id = newId, SubStrandId = subStrandIdMap[outcome.SubStrandId], Code = outcome.Code,
                Description = outcome.Description, DisplayOrder = outcome.DisplayOrder,
            });
        }

        foreach (var indicator in indicators)
        {
            dbContext.Indicators.Add(new Indicator
            {
                Id = Guid.NewGuid(), LearningOutcomeId = outcomeIdMap[indicator.LearningOutcomeId], Code = indicator.Code,
                Description = indicator.Description, DisplayOrder = indicator.DisplayOrder,
            });
        }

        foreach (var prerequisite in prerequisites)
        {
            // Both ends of a prerequisite link always belong to the same syllabus tree, so both
            // should always be present in the map — but skip defensively rather than throw if not.
            if (outcomeIdMap.TryGetValue(prerequisite.PrerequisiteLearningOutcomeId, out var newPrerequisiteId) &&
                outcomeIdMap.TryGetValue(prerequisite.LearningOutcomeId, out var newOutcomeId))
            {
                dbContext.LearningOutcomePrerequisites.Add(new LearningOutcomePrerequisite
                {
                    Id = Guid.NewGuid(), LearningOutcomeId = newOutcomeId, PrerequisiteLearningOutcomeId = newPrerequisiteId,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return newSyllabus.Id;
    }

    public async Task PublishAsync(Guid syllabusId, CancellationToken cancellationToken = default)
    {
        var syllabus = await dbContext.Syllabuses.FindAsync([syllabusId], cancellationToken)
            ?? throw new InvalidOperationException("Syllabus not found.");

        if (syllabus.Status != RecordStatus.Draft)
        {
            throw new InvalidOperationException("Only a Draft syllabus can be published.");
        }

        using (ImmutableRecordCorrectionScope.Enter())
        {
            if (syllabus.SupersedesId is { } supersededId)
            {
                var superseded = await dbContext.Syllabuses.FindAsync([supersededId], cancellationToken)
                    ?? throw new InvalidOperationException("Superseded syllabus not found.");

                superseded.IsCurrent = false;
                superseded.Status = RecordStatus.Superseded;
                superseded.SupersededById = syllabus.Id;
            }

            syllabus.Status = RecordStatus.Published;
            syllabus.IsCurrent = true;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
