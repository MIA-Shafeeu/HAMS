using HAMS.Platform.Common.Contracts;

namespace HAMS.PeopleEnrollment.Domain;

/// <summary>Configurable lookup (build plan §1.6 explicitly names "staff EmploymentStatus" as an example), not an enum.</summary>
public sealed class EmploymentStatus : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class EmploymentStatusCodes
{
    public const string Active = "ACTIVE";
    public const string OnLeave = "ON_LEAVE";
    public const string Resigned = "RESIGNED";
    public const string Retired = "RETIRED";
}
