using HAMS.CommunicationPortals.Domain;

namespace HAMS.CommunicationPortals.Application;

public interface IGuardianAcknowledgementService
{
    /// <summary>Idempotent — acknowledging the same (guardian, student, entity) pair twice returns the existing row, never inserts a second one.</summary>
    Task<Guid> AcknowledgeAsync(
        Guid guardianPersonId, Guid studentPersonId, string entityType, string entityId, CancellationToken cancellationToken = default);

    Task<GuardianAcknowledgement?> GetAsync(
        Guid guardianPersonId, Guid studentPersonId, string entityType, string entityId, CancellationToken cancellationToken = default);
}
