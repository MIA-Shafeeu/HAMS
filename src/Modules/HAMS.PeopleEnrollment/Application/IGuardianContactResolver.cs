namespace HAMS.PeopleEnrollment.Application;

public sealed record GuardianContact(Guid GuardianPersonId, string? PhoneNumber, string? Email);

/// <summary>
/// Resolves which guardians a student-facing notification (e.g. an absence alert) may actually be
/// sent to — the public read surface other modules (e.g. Attendance) consult via a
/// <c>ProjectReference</c>, the same pattern as OrgCurriculum's <c>ISchoolCalendarService</c>.
/// Deliberately narrow: only <see cref="GuardianStudentRelationship.CanReceiveNotifications"/>
/// guardians whose relationship is <see cref="GuardianVerificationStatus.Verified"/> and currently
/// active are ever returned — a claimed-but-unverified or notifications-opted-out relationship
/// must never receive one.
/// </summary>
public interface IGuardianContactResolver
{
    Task<IReadOnlyList<GuardianContact>> ResolveNotifiableGuardianContactsAsync(
        Guid studentPersonId, DateOnly asOf, CancellationToken cancellationToken = default);
}
