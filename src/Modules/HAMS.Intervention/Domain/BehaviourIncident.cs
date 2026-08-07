using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Domain;

namespace HAMS.Intervention.Domain;

/// <summary>
/// A recorded behaviour incident or commendation (build plan Phase 13 scope, 7.18) — the FOURTH real
/// <c>Platform.Workflow</c> consumer (after assessment moderation/topic closure/report cards),
/// reusing the exact same Draft→Submitted→UnderReview→Approved/Rejected/Returned pipeline with zero
/// kernel changes, same precedent every prior consumer already confirmed. <see cref="Description"/>
/// is deliberately single-language, matching <see cref="InterventionPlan.Description"/>'s established
/// precedent — a staff-internal record, not a bilingual published artifact. <see cref="SubjectId"/>
/// is nullable — a corridor/playground incident often has no associated subject class.
///
/// <see cref="ConfidentialityTierCode"/> plugs into the exact same <c>IConfidentialRecordAccessor"/>
/// kernel <see cref="InterventionCase"/> established in Phase 9 (build plan §2 explicitly calls both
/// domains' records "confidential sub-records feeding one student support lifecycle") — every
/// behaviour incident requires a tier, no unconditional/untiered path exists, matching
/// <see cref="InterventionCase"/>'s own unconditional requirement.
///
/// Deliberately NOT linked to <see cref="InterventionCase"/> by a hard FK in this phase — a reviewer
/// who judges a pattern of incidents warrants formal support opens a separate
/// <see cref="InterventionCase"/> for the same student through the existing Phase 9 path; the two
/// domains sharing one module and one confidentiality kernel is what "feeding one student support
/// lifecycle" means here, not a database-enforced pipeline between them.
/// </summary>
public sealed class BehaviourIncident : IScopedResource
{
    public Guid Id { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid BehaviourCategoryId { get; init; }

    public Guid? SubjectId { get; init; }

    public Guid AcademicYearId { get; init; }

    public required string Description { get; set; }

    public string? ActionTaken { get; set; }

    public required string ConfidentialityTierCode { get; init; }

    public Guid RecordedByPersonId { get; init; }

    public DateOnly OccurredDate { get; init; }

    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;

    public Guid? ReviewedByPersonId { get; set; }

    public string? ReviewNotes { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? DecidedAtUtc { get; set; }

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
