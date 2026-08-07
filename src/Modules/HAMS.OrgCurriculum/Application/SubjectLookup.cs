using HAMS.OrgCurriculum.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Application;

internal sealed class SubjectLookup(OrgDbContext dbContext) : ISubjectLookup
{
    public async Task<string?> GetNameAsync(Guid subjectId, CancellationToken cancellationToken = default)
        => await dbContext.Subjects.Where(s => s.Id == subjectId).Select(s => s.Name).SingleOrDefaultAsync(cancellationToken);
}
