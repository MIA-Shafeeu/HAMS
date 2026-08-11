namespace HAMS.Platform.Access;

/// <summary>
/// One <see cref="Domain.AccessGrant"/> row's scope dimensions, stripped down to just what a caller
/// outside this module needs to derive "what can this person reach" (see
/// <see cref="IPersonAccessScopeQuery"/>) — deliberately not the full entity, since
/// <see cref="Domain.AccessGrant"/> itself stays internal-detail shaped (role id, source
/// provenance) that no consumer of this summary needs. <see cref="RoleCode"/> IS included, unlike
/// the rest of that internal detail: a caller interpreting a grant whose <see cref="ClassId"/>/
/// <see cref="SubjectId"/> are both null needs to know WHICH role it came from before treating that
/// shape as "whole school" — the generic admin "Assign Role" form can produce that exact shape for
/// ANY role (it has no Class/Subject picker at all), so shape alone can't be trusted to mean
/// "this role is meant to see the whole school."
/// </summary>
public sealed record AccessGrantSummary(Guid? SchoolId, Guid? GradeId, Guid? ClassId, Guid? SubjectId, string RoleCode);

/// <summary>
/// Read side of the <see cref="Domain.AccessGrant"/> table for callers OUTSIDE Platform.Access that
/// need to know "what does this person's access look like," as opposed to
/// <see cref="Authorization.ScopeAuthorizationHandler"/>'s "does this person's access cover this
/// ONE already-known resource" — e.g. a module that needs to filter a picklist (which
/// schools/grades/classes to even show someone) rather than gate a single already-selected action.
/// </summary>
public interface IPersonAccessScopeQuery
{
    /// <summary>Every one of <paramref name="personId"/>'s <see cref="Domain.AccessGrant"/> rows active as of <paramref name="asOf"/>.</summary>
    Task<IReadOnlyList<AccessGrantSummary>> GetActiveGrantsAsync(Guid personId, DateOnly asOf, CancellationToken cancellationToken = default);
}
