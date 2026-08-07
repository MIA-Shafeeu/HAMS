namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// One of the National Curriculum Framework's eight Key Learning Areas (build plan §3/Phase 2
/// notes: Islam &amp; Spirituality, Language &amp; Communication, Mathematics, Environment/Science
/// &amp; Technology, Health &amp; Wellbeing, Social Sciences, Creative Arts, Entrepreneurship) — a
/// real, sourced seed set, not an invented placeholder. Also the unit elective-selection rules
/// reference (Key Stage 4 requires 4 electives from at least 2 Learning Areas including
/// Environment/Science &amp; Technology; Key Stage 5 requires 3), though enforcing that rule is
/// deferred to whichever later phase actually handles subject enrolment/electives.
/// </summary>
public sealed class LearningArea
{
    public Guid Id { get; init; }

    public Guid CurriculumFrameworkId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
