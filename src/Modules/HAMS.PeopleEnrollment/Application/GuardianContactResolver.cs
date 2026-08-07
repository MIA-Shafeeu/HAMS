using HAMS.PeopleEnrollment.Domain;
using HAMS.PeopleEnrollment.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.PeopleEnrollment.Application;

internal sealed class GuardianContactResolver(PeopleDbContext dbContext) : IGuardianContactResolver
{
    public async Task<IReadOnlyList<GuardianContact>> ResolveNotifiableGuardianContactsAsync(
        Guid studentPersonId, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await (
            from relationship in dbContext.GuardianStudentRelationships
                .Where(r => r.StudentPersonId == studentPersonId
                    && r.CanReceiveNotifications
                    && r.VerificationStatus == GuardianVerificationStatus.Verified)
                .ActiveAsOf(asOf)
            join guardian in dbContext.People on relationship.GuardianPersonId equals guardian.Id
            select new GuardianContact(guardian.Id, guardian.PhoneNumber, guardian.Email))
            .ToListAsync(cancellationToken);
    }
}
