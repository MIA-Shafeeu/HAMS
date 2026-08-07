namespace HAMS.Intervention.Domain;

/// <summary>
/// One remediation plan for an <see cref="InterventionCase"/> (build plan Phase 9 scope: "plans")
/// — what support will be provided, by whom, and by when. A case may accumulate more than one
/// plan over time (a first approach that didn't work, followed by a different one); the most
/// recent by creation order is the active plan, the same "current = latest" convention as every
/// other append-only history in this codebase (no separate "IsActive" flag needed).
/// </summary>
public sealed class InterventionPlan
{
    public Guid Id { get; init; }

    public Guid InterventionCaseId { get; init; }

    public required string Description { get; set; }

    public Guid AssignedStaffPersonId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly TargetDate { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}
