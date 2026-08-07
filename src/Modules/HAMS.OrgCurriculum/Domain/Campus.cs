namespace HAMS.OrgCurriculum.Domain;

public sealed class Campus
{
    public Guid Id { get; init; }

    public Guid SchoolId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;
}
