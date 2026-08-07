namespace HAMS.PeopleEnrollment.Domain;

/// <summary>Attaches student-specific fields to a <see cref="Person"/>. One-to-one — a person is at most one student.</summary>
public sealed class StudentProfile
{
    public Guid Id { get; init; }

    public Guid PersonId { get; init; }

    public required string AdmissionNumber { get; init; }

    public DateOnly AdmissionDate { get; set; }
}
