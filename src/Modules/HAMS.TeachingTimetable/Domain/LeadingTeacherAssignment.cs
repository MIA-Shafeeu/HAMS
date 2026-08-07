using HAMS.Platform.Common.Contracts;

namespace HAMS.TeachingTimetable.Domain;

/// <summary>
/// Subject-area leadership post (build plan §3, kept separate per TAS-FR-009/010) — scoped by
/// <see cref="SubjectId"/>, not by <c>LearningArea</c>: <c>AccessGrant</c>'s scope dimensions
/// (build plan §4) don't include a Learning Area column, and a leading teacher's oversight is
/// modelled as every class teaching that subject, across all grades, rather than widening the
/// core scope model for a single role. Grants the Leading Teacher role scoped to this subject
/// (grade/class left as wildcards).
/// </summary>
public sealed class LeadingTeacherAssignment : IEffectiveDated
{
    public Guid Id { get; init; }

    public Guid StaffPersonId { get; init; }

    public Guid SubjectId { get; init; }

    public Guid AcademicYearId { get; init; }

    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; set; }
}
