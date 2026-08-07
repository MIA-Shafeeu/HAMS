using System.Security.Claims;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Http;

namespace HAMS.IdentityAccess.Infrastructure;

/// <summary>
/// Reads the deliberately minimal claim set (<see cref="HamsClaimTypes"/>) off the current
/// request's <see cref="ClaimsPrincipal"/> — the sole implementation of Platform.Common's
/// <see cref="ICurrentUser"/> abstraction, so every module (including Platform.Access, which
/// cannot depend on IdentityAccess directly) can resolve "who is calling" without knowing how
/// authentication actually works.
/// </summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId => TryGetGuidClaim(ClaimTypes.NameIdentifier);

    public Guid? PersonId => TryGetGuidClaim(HamsClaimTypes.PersonId);

    public bool IsStaff => HasTrueClaim(HamsClaimTypes.IsStaff);

    public bool IsGuardian => HasTrueClaim(HamsClaimTypes.IsGuardian);

    public bool IsStudent => HasTrueClaim(HamsClaimTypes.IsStudent);

    public bool IsSystemAdmin => HasTrueClaim(HamsClaimTypes.IsSystemAdmin);

    private bool HasTrueClaim(string claimType) => Principal?.FindFirstValue(claimType) == "true";

    private Guid? TryGetGuidClaim(string claimType) =>
        Guid.TryParse(Principal?.FindFirstValue(claimType), out var value) ? value : null;
}
