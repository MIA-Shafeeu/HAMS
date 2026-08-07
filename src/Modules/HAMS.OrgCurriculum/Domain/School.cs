namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// The top of the org structure hierarchy (build plan §3). Kept even in a single-school
/// deployment — <see cref="Id"/> is threaded onto every other org entity so the schema is
/// multi-school-ready without any multi-tenant switching UX being built (Ruthless Cut #5).
/// </summary>
public sealed class School
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;
}
