namespace HAMS.OrgCurriculum.Domain;

public sealed class SubStrand
{
    public Guid Id { get; init; }

    public Guid StrandId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }
}
