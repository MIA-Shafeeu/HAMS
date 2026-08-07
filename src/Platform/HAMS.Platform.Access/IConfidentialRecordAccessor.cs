using HAMS.Platform.Common.Contracts;
using System.Security.Claims;

namespace HAMS.Platform.Access;

/// <summary>
/// The one chokepoint every read of a Restricted/Safeguarding record must go through (build plan
/// §4: BEH-FR-007/008/010, AC-019). Enforces the confidentiality grant <b>and</b> unconditionally
/// audit-logs the access — including denied attempts — regardless of outcome.
/// </summary>
public interface IConfidentialRecordAccessor
{
    /// <returns>True if access is authorized. The access attempt is audit-logged either way.</returns>
    Task<bool> CanAccessAsync(
        ClaimsPrincipal user, IScopedResource resource, string entityType, string entityId,
        CancellationToken cancellationToken = default);
}
