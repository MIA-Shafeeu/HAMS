using HAMS.PeopleEnrollment.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.PeopleEnrollment.Application;

internal sealed class StudentProfileLookup(PeopleDbContext dbContext) : IStudentProfileLookup
{
    public async Task<Guid?> FindPersonIdByAdmissionNumberAsync(string admissionNumber, CancellationToken cancellationToken = default)
        => await dbContext.StudentProfiles
            .Where(p => p.AdmissionNumber == admissionNumber)
            .Select(p => (Guid?)p.PersonId)
            .SingleOrDefaultAsync(cancellationToken);
}
