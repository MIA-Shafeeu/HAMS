namespace HAMS.PeopleEnrollment.Domain;

/// <summary>
/// A structural workflow state, not business/reference data — a genuine C# enum by the same
/// exception as <c>RecordStatus</c> (Platform.Common): the school's confirmation process for a
/// claimed guardian relationship always means the same thing to the code (only "Verified"
/// relationships should ever be treated as authoritative for portal access once guardian login
/// exists), and extending it would need new code paths regardless of storage.
/// </summary>
public enum GuardianVerificationStatus
{
    Pending = 0,
    Verified = 1,
    Rejected = 2,
}
