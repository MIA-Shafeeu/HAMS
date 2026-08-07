namespace HAMS.Platform.Common.Contracts;

/// <summary>
/// The resource-side adapter every authorizable entity implements so a single generic
/// <c>ScopeAuthorizationHandler</c> (Platform.Access) can check it against a caller's
/// <c>AccessGrant</c> rows, instead of each module writing its own bespoke authorization
/// handler. Implements SRS §5's Access Scope: Role + School/Campus + Academic Year + Key Stage
/// + Grade + Class + Subject + Assignment + Student/Guardian Relationship + Confidentiality +
/// Effective Date.
///
/// Every dimension is nullable by design: a populated value means "this resource is scoped to
/// this specific X," and a null value means "this dimension doesn't restrict access to this
/// resource" (a wildcard), not "unknown." Platform.Common deliberately has no compile-time
/// reference to the entities these IDs point at (School, Grade, Subject, etc. are defined in
/// business modules built later) — it only needs the shape.
/// </summary>
public interface IScopedResource
{
    Guid? SchoolId { get; }
    Guid? CampusId { get; }
    Guid? AcademicYearId { get; }
    Guid? KeyStageId { get; }
    Guid? GradeId { get; }
    Guid? ClassId { get; }
    Guid? SubjectId { get; }
    Guid? StudentId { get; }

    /// <summary>
    /// The stable <c>Code</c> of the resource's confidentiality tier (per the configurable
    /// <c>ConfidentialityTier</c> lookup entity, §1.6 — never an enum). Null means ordinary,
    /// non-confidential data that the generic scope check alone governs. A non-null tier means
    /// the <c>ConfidentialityAuthorizationHandler</c>'s separate, always-explicit check also
    /// applies — confidentiality is AND-ed on top of scope, never implied by role membership.
    /// </summary>
    string? ConfidentialityTierCode { get; }
}
