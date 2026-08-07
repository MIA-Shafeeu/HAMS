namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// Records that a given calendar day-of-week is a working/school day for a school — the
/// configurable half of the school week (build plan §1.6, per explicit user instruction):
/// <see cref="DayOfWeek"/> itself (which of the seven named days something is) is a structural,
/// universal calendar fact and stays the BCL enum, but *which* of those seven days a school
/// actually operates on is real business data that varies by country/school and must never be
/// hardcoded — the Maldivian working week is Sunday-Thursday (Friday/Saturday weekend), not the
/// Monday-Friday week much scheduling logic silently assumes. A school's existence is what
/// determines the default set of rows (seeded at school-creation time — see
/// <c>OrgEndpoints</c> — not baked into any enum or code branch), and admins can freely add or
/// remove rows afterwards through the API.
/// </summary>
public sealed class WorkingDay
{
    public Guid Id { get; init; }

    public Guid SchoolId { get; init; }

    public DayOfWeek DayOfWeek { get; init; }
}
