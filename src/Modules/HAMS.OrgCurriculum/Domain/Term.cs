namespace HAMS.OrgCurriculum.Domain;

public sealed class Term
{
    public Guid Id { get; init; }

    public Guid AcademicYearId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int DisplayOrder { get; set; }
}
