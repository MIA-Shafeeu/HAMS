namespace HAMS.PeopleEnrollment.Domain;

public sealed class StaffQualification
{
    public Guid Id { get; init; }

    public Guid StaffProfileId { get; init; }

    public required string Title { get; set; }

    public string? AwardingInstitution { get; set; }

    public int? YearAwarded { get; set; }
}
