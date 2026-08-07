using HAMS.PeopleEnrollment.Application;

namespace HAMS.Attendance.Tests;

/// <summary>Defaults to no notifiable guardians — pass specific contacts to test the absence-notification path.</summary>
internal sealed class FakeGuardianContactResolver(params GuardianContact[] contacts) : IGuardianContactResolver
{
    public Task<IReadOnlyList<GuardianContact>> ResolveNotifiableGuardianContactsAsync(
        Guid studentPersonId, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GuardianContact>>(contacts);
}
