namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// A teaching group. May combine more than one <see cref="Grade"/> via <see cref="ClassGrade"/>
/// (ORG-FR-018 — small island schools like Hirilandhoo routinely mix grades due to low enrolment).
/// Evaluation logic must never resolve a student's key-stage policy from this entity — always
/// from <c>StudentEnrollment.GradeId</c> — or a combined-class student would silently inherit the
/// other grade's evaluation model (build plan §12).
/// </summary>
public sealed class Class
{
    public Guid Id { get; init; }

    public Guid SchoolId { get; init; }

    public Guid? CampusId { get; init; }

    public Guid AcademicYearId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    /// <summary>"#RRGGBB" swatch this class renders as on the whole-school timetable calendar — an admin-set display attribute, not a business/scoping rule.</summary>
    public required string ColorHex { get; set; }

    public bool IsActive { get; set; } = true;
}
