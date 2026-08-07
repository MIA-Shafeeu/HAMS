using HAMS.PeopleEnrollment.Application;

namespace HAMS.AssessmentEvaluation.Tests;

/// <summary>Defaults to no notifiable guardians — pass specific contacts to test the result-publication notification path. Mirrors HAMS.Attendance.Tests' identical fake.</summary>
internal sealed class FakeGuardianContactResolver(params GuardianContact[] contacts) : IGuardianContactResolver
{
    public Task<IReadOnlyList<GuardianContact>> ResolveNotifiableGuardianContactsAsync(
        Guid studentPersonId, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GuardianContact>>(contacts);
}
