namespace HAMS.PeopleEnrollment.Domain;

/// <summary>Attaches staff-specific fields to a <see cref="Person"/>. One-to-one — a person is at most one staff record.</summary>
public sealed class StaffProfile
{
    public Guid Id { get; init; }

    public Guid PersonId { get; init; }

    public required string EmployeeNumber { get; init; }

    public DateOnly HireDate { get; set; }

    public Guid EmploymentStatusId { get; set; }
}
