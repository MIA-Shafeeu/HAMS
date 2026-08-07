using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HAMS.Platform.Access.Infrastructure;

internal sealed class ConfidentialRecordAccessor(
    IAuthorizationService authorizationService,
    IAuditLogWriter auditLogWriter,
    ICurrentUser currentUser,
    IClock clock)
    : IConfidentialRecordAccessor
{
    public async Task<bool> CanAccessAsync(
        ClaimsPrincipal user, IScopedResource resource, string entityType, string entityId,
        CancellationToken cancellationToken = default)
    {
        var result = await authorizationService.AuthorizeAsync(user, resource, PlatformAccessPolicies.Confidentiality);

        await auditLogWriter.WriteAsync(new AuditLogEntry
        {
            OccurredAtUtc = clock.UtcNow,
            Action = result.Succeeded ? AuditAction.Read : AuditAction.PermissionDenied,
            EntityType = entityType,
            EntityId = entityId,
            ActorPersonId = currentUser.PersonId,
            ActorUserId = currentUser.UserId,
            Summary = result.Succeeded
                ? $"Confidential {entityType} record accessed."
                : $"Confidential {entityType} record access denied.",
        }, cancellationToken);

        return result.Succeeded;
    }
}
