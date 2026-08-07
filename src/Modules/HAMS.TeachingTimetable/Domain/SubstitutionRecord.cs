namespace HAMS.TeachingTimetable.Domain;

/// <summary>
/// Records why a single-day <see cref="SubjectTeachingAssignment"/> (with
/// <see cref="AssignmentRoleCodes.Substitute"/>) exists — the generated assignment,
/// <see cref="GeneratedAssignmentId"/>, is what actually grants access; auto-expiry falls out for
/// free from its own single-day <c>EffectiveFrom</c>/<c>EffectiveTo</c> window (build plan §3/§4
/// — "no scheduled revocation job required"), so this record itself needs no expiry logic.
/// </summary>
public sealed class SubstitutionRecord
{
    public Guid Id { get; init; }

    public Guid OriginalAssignmentId { get; init; }

    public Guid SubstituteStaffPersonId { get; init; }

    public DateOnly SubstitutionDate { get; init; }

    public Guid GeneratedAssignmentId { get; init; }

    public string? Reason { get; set; }
}
