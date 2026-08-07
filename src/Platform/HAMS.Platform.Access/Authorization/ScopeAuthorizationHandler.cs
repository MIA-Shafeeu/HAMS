using HAMS.Platform.Access.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access.Authorization;

/// <summary>Marker requirement — all the actual matching logic lives in the handler, keyed off the resource.</summary>
public sealed class ScopeRequirement : IAuthorizationRequirement
{
    public static readonly ScopeRequirement Instance = new();
}

/// <summary>
/// The one generic scope handler every resource in the system is checked against (build plan §4)
/// — not a bespoke handler per module. For each dimension the resource populates, at least one of
/// the caller's live, effective-dated <c>AccessGrant</c> rows must either wildcard that dimension
/// (null) or match it exactly. Deliberately does not call <c>context.Fail()</c> on a non-match:
/// that would short-circuit other handlers evaluating the same requirement, when "not authorized"
/// should simply fall out of no handler having called <c>Succeed</c>.
/// </summary>
public sealed class ScopeAuthorizationHandler(AccessDbContext dbContext, ICurrentUser currentUser, IClock clock)
    : AuthorizationHandler<ScopeRequirement, IScopedResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ScopeRequirement requirement, IScopedResource resource)
    {
        if (currentUser.PersonId is not { } personId)
        {
            return;
        }

        var hasMatchingGrant = await dbContext.AccessGrants
            .Where(g => g.PersonId == personId)
            .ActiveAsOf(clock.TodayUtc)
            .AnyAsync(g =>
                (resource.SchoolId == null || g.SchoolId == null || g.SchoolId == resource.SchoolId) &&
                (resource.CampusId == null || g.CampusId == null || g.CampusId == resource.CampusId) &&
                (resource.AcademicYearId == null || g.AcademicYearId == null || g.AcademicYearId == resource.AcademicYearId) &&
                (resource.KeyStageId == null || g.KeyStageId == null || g.KeyStageId == resource.KeyStageId) &&
                (resource.GradeId == null || g.GradeId == null || g.GradeId == resource.GradeId) &&
                (resource.ClassId == null || g.ClassId == null || g.ClassId == resource.ClassId) &&
                (resource.SubjectId == null || g.SubjectId == null || g.SubjectId == resource.SubjectId) &&
                (resource.StudentId == null || g.StudentId == null || g.StudentId == resource.StudentId));

        if (hasMatchingGrant)
        {
            context.Succeed(requirement);
        }
    }
}
