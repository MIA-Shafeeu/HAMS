using HAMS.Platform.Common.Contracts;

namespace HAMS.PeopleEnrollment.Domain;

/// <summary>
/// One row per enrolment period (build plan §3) — <see cref="GradeId"/>/<see cref="ClassId"/>/
/// <see cref="AcademicYearId"/> are loose references into OrgCurriculum's "org" schema (modules
/// never take a hard cross-schema FK, per build plan §2). This is the row the evaluation-model
/// cascade resolves from: <c>StudentEnrollment.GradeId -&gt; GradeKeyStageAssignment.KeyStageId -&gt;
/// KeyStagePolicy</c> — never from <c>ClassId</c>, so a combined-grade class never leaks one
/// grade's policy onto the other's students.
///
/// A filtered unique index (see <c>PeopleDbContext</c>) enforces ORG-FR-017: at most one currently
/// active <see cref="EnrollmentTypeCodes.Ordinary"/> enrolment per student per academic year.
/// </summary>
public sealed class StudentEnrollment : IEffectiveDated
{
    public Guid Id { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid GradeId { get; init; }

    public Guid ClassId { get; init; }

    public Guid AcademicYearId { get; init; }

    public Guid EnrollmentTypeId { get; init; }

    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; set; }
}
