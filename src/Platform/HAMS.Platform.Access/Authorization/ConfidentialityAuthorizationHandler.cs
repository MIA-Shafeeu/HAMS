using HAMS.Platform.Access.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access.Authorization;

public sealed class ConfidentialityRequirement : IAuthorizationRequirement
{
    public static readonly ConfidentialityRequirement Instance = new();
}

/// <summary>
/// The second, always-explicit, AND-ed confidentiality check (build plan §4) — never implied by
/// ordinary role/scope membership. A resource with a null <c>ConfidentialityTierCode</c> isn't
/// confidential at all, so this check trivially succeeds and leaves gatekeeping entirely to
/// <see cref="ScopeAuthorizationHandler"/>. A non-null tier requires a live
/// <c>ConfidentialAccessGrant</c> whose tier rank covers the resource's required tier.
/// </summary>
public sealed class ConfidentialityAuthorizationHandler(AccessDbContext dbContext, ICurrentUser currentUser, IClock clock)
    : AuthorizationHandler<ConfidentialityRequirement, IScopedResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ConfidentialityRequirement requirement, IScopedResource resource)
    {
        if (resource.ConfidentialityTierCode is null)
        {
            context.Succeed(requirement);
            return;
        }

        if (currentUser.PersonId is not { } personId)
        {
            return;
        }

        var requiredRank = await dbContext.ConfidentialityTiers
            .Where(t => t.Code == resource.ConfidentialityTierCode)
            .Select(t => (int?)t.Rank)
            .SingleOrDefaultAsync();

        if (requiredRank is null)
        {
            return;
        }

        var hasCoveringGrant = await dbContext.ConfidentialAccessGrants
            .Where(g => g.PersonId == personId)
            .ActiveAsOf(clock.TodayUtc)
            .Where(g => resource.StudentId == null || g.StudentId == null || g.StudentId == resource.StudentId)
            .Join(dbContext.ConfidentialityTiers, g => g.ConfidentialityTierId, t => t.Id, (g, t) => t.Rank)
            .AnyAsync(rank => rank >= requiredRank);

        if (hasCoveringGrant)
        {
            context.Succeed(requirement);
        }
    }
}
