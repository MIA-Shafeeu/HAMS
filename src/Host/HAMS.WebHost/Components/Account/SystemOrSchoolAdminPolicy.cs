using HAMS.IdentityAccess.Application.Jwt;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Authorization;

namespace HAMS.WebHost.Components.Account;

/// <summary>
/// Real admin gating for the Phase 12 Blazor pages (Dashboard/Audit Log/Regulatory Reports). A bare
/// <c>[Authorize]</c> only proves the caller is SOME authenticated principal — since guardians and
/// students hold real JWTs/cookies too (one issuance path for every principal type, build plan §5),
/// that alone let any authenticated staff member of ANY role reach these admin-only pages. A
/// scheme-restricted <c>[Authorize(AuthenticationSchemes = ...)]</c> can't be used on a Razor
/// component (Blazor throws "Authentication schemes cannot be specified for components"), but a
/// named POLICY works fine — <c>[Authorize(Policy = SystemOrSchoolAdminPolicy.Name)]</c> resolves
/// through ASP.NET Core's ordinary <c>IAuthorizationService</c>, which correctly supplies the
/// cascaded <c>ClaimsPrincipal</c> regardless of static or interactive render mode.
/// </summary>
public static class SystemOrSchoolAdminPolicy
{
    public const string Name = "HAMS.SystemOrSchoolAdmin";
}

public sealed class SystemOrSchoolAdminRequirement : IAuthorizationRequirement
{
    public static readonly SystemOrSchoolAdminRequirement Instance = new();
}

/// <summary>
/// Reads the person id directly off the <see cref="AuthorizationHandlerContext.User"/> the
/// authorization pipeline supplies — deliberately NOT <see cref="ICurrentUser"/>
/// (<c>IHttpContextAccessor</c>-backed), which isn't reliably populated once a Blazor Server
/// interactive circuit is running. Still always a live <see cref="IRoleMembershipQuery"/> check,
/// never a cached JWT/cookie claim (build plan §4's standing rule: authorization is never decided
/// from the token alone).
/// </summary>
internal sealed class SystemOrSchoolAdminAuthorizationHandler(IRoleMembershipQuery roleMembershipQuery, IClock clock)
    : AuthorizationHandler<SystemOrSchoolAdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SystemOrSchoolAdminRequirement requirement)
    {
        var personIdClaim = context.User.FindFirst(HamsClaimTypes.PersonId)?.Value;
        if (Guid.TryParse(personIdClaim, out var personId)
            && await roleMembershipQuery.IsSystemOrSchoolAdminAsync(personId, clock.TodayUtc))
        {
            context.Succeed(requirement);
        }
    }
}
