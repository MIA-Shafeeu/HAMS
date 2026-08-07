using HAMS.CommunicationPortals.Domain;
using HAMS.CommunicationPortals.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.CommunicationPortals.Application;

internal sealed class GuardianAcknowledgementService(CommunicationPortalsDbContext dbContext, IClock clock) : IGuardianAcknowledgementService
{
    public async Task<Guid> AcknowledgeAsync(
        Guid guardianPersonId, Guid studentPersonId, string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        var existing = await FindAsync(guardianPersonId, studentPersonId, entityType, entityId, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var acknowledgement = new GuardianAcknowledgement
        {
            Id = Guid.NewGuid(), GuardianPersonId = guardianPersonId, StudentPersonId = studentPersonId,
            EntityType = entityType, EntityId = entityId, AcknowledgedAtUtc = clock.UtcNow,
        };
        dbContext.GuardianAcknowledgements.Add(acknowledgement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return acknowledgement.Id;
    }

    public Task<GuardianAcknowledgement?> GetAsync(
        Guid guardianPersonId, Guid studentPersonId, string entityType, string entityId, CancellationToken cancellationToken = default)
        => FindAsync(guardianPersonId, studentPersonId, entityType, entityId, cancellationToken);

    private Task<GuardianAcknowledgement?> FindAsync(
        Guid guardianPersonId, Guid studentPersonId, string entityType, string entityId, CancellationToken cancellationToken)
        => dbContext.GuardianAcknowledgements.SingleOrDefaultAsync(
            a => a.GuardianPersonId == guardianPersonId && a.StudentPersonId == studentPersonId && a.EntityType == entityType && a.EntityId == entityId,
            cancellationToken);
}
