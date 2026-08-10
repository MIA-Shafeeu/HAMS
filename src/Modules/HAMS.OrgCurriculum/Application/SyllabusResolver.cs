using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Application;

internal sealed class SyllabusResolver(OrgDbContext dbContext) : ISyllabusResolver
{
    public Task<Syllabus?> ResolveAsync(Guid subjectId, Guid gradeId, CancellationToken cancellationToken = default)
    {
        return dbContext.Syllabuses
            .Where(s => s.SubjectId == subjectId && s.IsCurrent)
            .Where(s => s.Status == RecordStatus.Published || s.Status == RecordStatus.Locked)
            .Where(s => dbContext.SyllabusGradeApplicabilities.Any(a => a.SyllabusId == s.Id && a.GradeId == gradeId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetLearningOutcomeIdsAsync(Guid syllabusId, CancellationToken cancellationToken = default)
        => await (
            from strand in dbContext.Strands
            where strand.SyllabusId == syllabusId
            join subStrand in dbContext.SubStrands on strand.Id equals subStrand.StrandId
            join outcome in dbContext.LearningOutcomes on subStrand.Id equals outcome.SubStrandId
            select outcome.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LearningOutcomeOption>> GetLearningOutcomeOptionsAsync(Guid syllabusId, CancellationToken cancellationToken = default)
        => await (
            from strand in dbContext.Strands
            where strand.SyllabusId == syllabusId
            join subStrand in dbContext.SubStrands on strand.Id equals subStrand.StrandId
            join outcome in dbContext.LearningOutcomes on subStrand.Id equals outcome.SubStrandId
            orderby strand.DisplayOrder, subStrand.DisplayOrder, outcome.DisplayOrder
            select new LearningOutcomeOption(outcome.Id, strand.Name, subStrand.Name, outcome.Code, outcome.Description))
            .ToListAsync(cancellationToken);
}
