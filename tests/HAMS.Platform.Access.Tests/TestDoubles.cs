using HAMS.Platform.Common.Contracts;

namespace HAMS.Platform.Access.Tests;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public bool IsAuthenticated => PersonId is not null;
    public Guid? UserId { get; init; }
    public Guid? PersonId { get; init; }
    public bool IsStaff { get; init; } = true;
    public bool IsGuardian { get; init; }
    public bool IsStudent { get; init; }
    public bool IsSystemAdmin { get; init; }
}

internal sealed class FakeClock(DateOnly today) : IClock
{
    public DateTimeOffset UtcNow => today.ToDateTime(TimeOnly.MinValue);
    public DateOnly TodayUtc => today;
}

internal sealed record FakeScopedResource(
    Guid? SchoolId = null, Guid? CampusId = null, Guid? AcademicYearId = null, Guid? KeyStageId = null,
    Guid? GradeId = null, Guid? ClassId = null, Guid? SubjectId = null, Guid? StudentId = null,
    string? ConfidentialityTierCode = null) : IScopedResource;
