namespace HAMS.IdentityAccess.Domain;

/// <summary>
/// The build plan's "custom lightweight OTP store" (§5) — a short-lived, one-time code issued to a
/// guardian's phone number to prove they control it. Deliberately not delegated to Message Owl's
/// own hosted OTP endpoints (<c>otp.msgowl.com/send|resend|verify</c>): building this store
/// ourselves, on top of the plain <c>ISmsSender.SendAsync</c> the same way every other SMS in this
/// codebase is sent, keeps OTP logic carrier-agnostic — if Dhiraagu/Ooredoo direct integration ever
/// replaces Msgowl, nothing here needs to change.
///
/// <see cref="CodeHash"/> follows the exact same "never persist the secret itself" discipline as
/// <c>UserSession.RefreshTokenHash</c>. Proving control of a phone number is <b>not</b> the same as
/// proving a legal guardian relationship — this challenge only ever authenticates a phone number
/// against whichever <c>Person</c> a school administrator already marked
/// <see cref="HAMS.PeopleEnrollment.Domain.GuardianVerificationStatus.Verified"/> for that student;
/// it never itself flips a relationship's verification status.
/// </summary>
public sealed class GuardianOtpChallenge
{
    public Guid Id { get; init; }

    public Guid PersonId { get; init; }

    public required string PhoneNumber { get; init; }

    public required string CodeHash { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Mutable, not just init: requesting a fresh code retroactively expires any prior outstanding one for the same phone number.</summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? ConsumedAtUtc { get; set; }
}
