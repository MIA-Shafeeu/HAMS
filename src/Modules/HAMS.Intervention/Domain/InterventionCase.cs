using HAMS.Platform.Common.Contracts;

namespace HAMS.Intervention.Domain;

/// <summary>
/// A student needing additional support in a subject (build plan §2/Phase 9 scope: "Cases").
/// <see cref="LearningOutcomeId"/> is set when the trigger was a Mastery-model gap in one specific
/// outcome; left null when the trigger was a whole-subject Assessment-model result.
/// <see cref="TriggeringKeyStageEvaluationId"/>/<see cref="CarriedForwardGapId"/> are optional —
/// a case can be opened directly from a low <c>KeyStageEvaluation</c> (Phase 8), from a
/// <see cref="CarriedForwardGap"/> left by a topic closure, or manually with neither.
///
/// <see cref="ConfidentialityTierCode"/> stores <c>Platform.Access</c>'s <c>ConfidentialityTierCodes</c>
/// constant directly (the exact string <see cref="IScopedResource.ConfidentialityTierCode"/> expects) —
/// the build plan explicitly calls Intervention's records "confidential sub-records" (§2), and
/// this is the first entity in the codebase to actually plug into that kernel; every read of a
/// single case goes through <c>IConfidentialRecordAccessor</c>, never a plain lookup. Only the
/// student/subject/year/confidentiality dimensions are populated on <see cref="IScopedResource"/> —
/// a case has no school/campus/key-stage/grade/class dimension of its own.
/// </summary>
public sealed class InterventionCase : IScopedResource
{
    public Guid Id { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid SubjectId { get; init; }

    public Guid AcademicYearId { get; init; }

    public Guid? LearningOutcomeId { get; init; }

    public Guid? TriggeringKeyStageEvaluationId { get; init; }

    public Guid? CarriedForwardGapId { get; set; }

    public Guid InterventionTypeId { get; set; }

    public required string ConfidentialityTierCode { get; init; }

    public Guid OpenedByPersonId { get; init; }

    public DateOnly OpenedDate { get; init; }

    public InterventionCaseStatus Status { get; set; } = InterventionCaseStatus.Open;

    public DateOnly? ClosedDate { get; set; }

    Guid? IScopedResource.SchoolId => null;
    Guid? IScopedResource.CampusId => null;
    Guid? IScopedResource.AcademicYearId => AcademicYearId;
    Guid? IScopedResource.KeyStageId => null;
    Guid? IScopedResource.GradeId => null;
    Guid? IScopedResource.ClassId => null;
    Guid? IScopedResource.SubjectId => SubjectId;
    Guid? IScopedResource.StudentId => StudentPersonId;
    string? IScopedResource.ConfidentialityTierCode => ConfidentialityTierCode;
}
