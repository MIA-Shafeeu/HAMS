using HAMS.Platform.Common.Contracts;

namespace HAMS.TeachingTimetable.Domain;

/// <summary>
/// One of the three concrete teaching-assignment tables (build plan §3, TAS-FR-009/010 — kept
/// separate from <see cref="ClassTeacherAssignment"/>/<see cref="LeadingTeacherAssignment"/>
/// rather than one polymorphic table). Grants the Subject Teacher role scoped to exactly this
/// subject+class via <c>IScopedAccessGrantProjector</c>, synchronously with this row's own write.
///
/// A <see cref="SubstitutionRecord"/> generates one of these rows too, with
/// <see cref="AssignmentRoleId"/> = <see cref="AssignmentRoleCodes.Substitute"/> and a single-day
/// <see cref="IEffectiveDated.EffectiveFrom"/>/<see cref="IEffectiveDated.EffectiveTo"/> window —
/// the exact same table, the exact same granted role, no special-case authorization path.
/// </summary>
public sealed class SubjectTeachingAssignment : IEffectiveDated
{
    public Guid Id { get; init; }

    public Guid StaffPersonId { get; init; }

    public Guid SubjectId { get; init; }

    public Guid ClassId { get; init; }

    public Guid AcademicYearId { get; init; }

    public Guid AssignmentRoleId { get; init; }

    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; set; }
}
