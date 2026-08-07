namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// The master catalogue entry for a taught subject (build plan §3) — key-stage-independent by
/// design. Which grades/key stages actually teach it, and with what content, is determined by
/// which <see cref="Syllabus"/>/<see cref="SyllabusGradeApplicability"/> rows exist, not by any
/// field here.
/// </summary>
public sealed class Subject
{
    public Guid Id { get; init; }

    public Guid SchoolId { get; init; }

    public Guid LearningAreaId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    /// <summary>FK to <see cref="DeliveryMode"/> — Timetabled vs Integrated (e.g. ICT below Key Stage 4).</summary>
    public Guid DeliveryModeId { get; set; }

    /// <summary>FK to <see cref="MediumOfInstruction"/> — the per-subject override (build plan §3/§7).</summary>
    public Guid DefaultMediumOfInstructionId { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
