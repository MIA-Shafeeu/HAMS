using HAMS.Platform.Common.Contracts;

namespace HAMS.TeachingTimetable.Domain;

/// <summary>Homeroom/class-teacher assignment (build plan §3, kept separate per TAS-FR-009/010). Grants the Class Teacher role scoped to this class.</summary>
public sealed class ClassTeacherAssignment : IEffectiveDated
{
    public Guid Id { get; init; }

    public Guid StaffPersonId { get; init; }

    public Guid ClassId { get; init; }

    public Guid AcademicYearId { get; init; }

    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; set; }
}
