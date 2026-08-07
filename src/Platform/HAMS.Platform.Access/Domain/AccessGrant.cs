using HAMS.Platform.Common.Contracts;

namespace HAMS.Platform.Access.Domain;

/// <summary>
/// The one derived projection table the whole permission model is built on (build plan §4).
/// Every dimension below is nullable = wildcard: a null <see cref="GradeId"/> means "this grant
/// isn't restricted by grade," not "unknown." <see cref="ScopeAuthorizationHandler"/> checks a
/// target <c>IScopedResource</c> against these rows; it never re-derives scope from source
/// tables at request time.
///
/// Upserted synchronously, in the same transaction as whatever source row changed, by
/// <see cref="IAccessGrantProjectionService"/> — never eventually-consistent. In Phase 1 the only
/// source is <see cref="PersonRoleAssignment"/>; later phases add
/// <c>SubjectTeachingAssignment</c>/<c>GuardianStudentRelationship</c>/etc. as further sources,
/// each still writing into this same table.
/// </summary>
public sealed class AccessGrant : IEffectiveDated
{
    public Guid Id { get; init; }

    public Guid PersonId { get; init; }

    public Guid RoleId { get; init; }

    public Guid? SchoolId { get; init; }
    public Guid? CampusId { get; init; }
    public Guid? AcademicYearId { get; init; }
    public Guid? KeyStageId { get; init; }
    public Guid? GradeId { get; init; }
    public Guid? ClassId { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid? StudentId { get; init; }

    /// <summary>
    /// FK to <see cref="ConfidentialityTier"/>. Null does NOT mean "no confidentiality
    /// restriction" here the way it does on <c>IScopedResource</c> — a grant either does or
    /// doesn't carry confidential access; ordinary grants simply leave this null and never
    /// participate in a confidentiality check (that check is AND-ed on separately via
    /// <see cref="ConfidentialAccessGrant"/>, not via this column).
    /// </summary>
    public Guid? ConfidentialityTierId { get; init; }

    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>
    /// Small, stable provenance discriminator (exempt from the no-enums principle per build plan
    /// §4 — internal debugging/audit metadata, not user-facing configuration). See
    /// <see cref="AccessGrantSourceTypes"/>.
    /// </summary>
    public required string SourceType { get; init; }

    /// <summary>The id of the source row (e.g. the <see cref="PersonRoleAssignment"/>) this grant was projected from.</summary>
    public Guid SourceId { get; init; }
}

public static class AccessGrantSourceTypes
{
    public const string PersonRoleAssignment = "PersonRoleAssignment";
    public const string Delegation = "Delegation";
    public const string SubjectTeachingAssignment = "SubjectTeachingAssignment";
    public const string ClassTeacherAssignment = "ClassTeacherAssignment";
    public const string LeadingTeacherAssignment = "LeadingTeacherAssignment";
}
