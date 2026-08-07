using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Application;

internal sealed class KeyStagePolicyResolver(OrgDbContext dbContext) : IKeyStagePolicyResolver
{
    public async Task<KeyStagePolicy?> ResolveAsync(Guid gradeId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var keyStageId = await dbContext.GradeKeyStageAssignments
            .Where(a => a.GradeId == gradeId && a.AcademicYearId == academicYearId)
            .ActiveAsOf(asOf)
            .Select(a => (Guid?)a.KeyStageId)
            .SingleOrDefaultAsync(cancellationToken);

        if (keyStageId is null)
        {
            return null;
        }

        // Only a Published/Locked policy is actually in force — a Draft correction-in-progress
        // must never silently apply to live evaluations (build plan §3).
        return await dbContext.KeyStagePolicies
            .Where(p => p.KeyStageId == keyStageId && p.AcademicYearId == academicYearId)
            .Where(p => p.IsCurrent && (p.Status == RecordStatus.Published || p.Status == RecordStatus.Locked))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<KeyStagePolicy?> GetByIdAsync(Guid keyStagePolicyId, CancellationToken cancellationToken = default)
        => await dbContext.KeyStagePolicies.FindAsync([keyStagePolicyId], cancellationToken);
}
