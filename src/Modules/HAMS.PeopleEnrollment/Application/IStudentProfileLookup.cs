namespace HAMS.PeopleEnrollment.Application;

/// <summary>
/// The one small read IdentityAccess's student ID+PIN login (Phase 10) needs — resolving the
/// <c>Person</c> behind a student's own <c>AdmissionNumber</c> ("Student ID"), the natural login
/// identifier since students don't have a username of their own otherwise.
/// </summary>
public interface IStudentProfileLookup
{
    /// <returns>Null if no student has this admission number.</returns>
    Task<Guid?> FindPersonIdByAdmissionNumberAsync(string admissionNumber, CancellationToken cancellationToken = default);
}
