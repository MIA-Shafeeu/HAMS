namespace HAMS.PeopleEnrollment.Application;

public sealed record EstablishGuardianRelationshipRequest(
    Guid GuardianPersonId, Guid StudentPersonId, Guid RelationshipTypeId, bool HasLegalAuthority,
    bool CanViewAcademicRecords, bool CanViewAttendance, bool CanViewBehaviourRecords, bool CanViewInterventionUpdates,
    bool CanReceiveNotifications, Guid? RestrictionTypeId, DateOnly EffectiveFrom);

public sealed record ReviseGuardianRelationshipRequest(
    Guid RelationshipTypeId, bool HasLegalAuthority, bool CanViewAcademicRecords, bool CanViewAttendance,
    bool CanViewBehaviourRecords, bool CanViewInterventionUpdates, bool CanReceiveNotifications, Guid? RestrictionTypeId);

/// <summary>
/// A student one guardian may see in the portal, and exactly what they're permitted to see about
/// them (Phase 10). <see cref="CanViewBehaviourRecords"/> defaults to <see langword="false"/> only
/// so every existing 4-argument call site (predating Phase 13's behaviour/pastoral feature) keeps
/// compiling unchanged — <see cref="GuardianRelationshipService.GetStudentsForGuardianAsync"/>
/// itself always supplies the real value. <see cref="NameEn"/>/<see cref="NameDv"/>/<see cref="AdmissionNumber"/>
/// (Phase C2) default to "" for the same reason, and are resolved inside
/// <see cref="GuardianRelationshipService.GetStudentsForGuardianAsync"/> itself — via a left join
/// against this same module's own <c>Person</c>/<c>StudentProfile</c> tables, already scoped to
/// relationships this guardian is Verified for — deliberately NOT by having a guardian-facing client
/// call the general-purpose <c>GET /api/v1/people/persons/{personId}</c> admin lookup, which performs
/// no relationship check at all and would let a guardian's token read any person's full profile by GUID.
/// </summary>
public sealed record GuardianStudentSummary(
    Guid StudentPersonId, bool CanViewAcademicRecords, bool CanViewAttendance, bool CanViewInterventionUpdates,
    bool CanViewBehaviourRecords = false, string NameEn = "", string NameDv = "", string AdmissionNumber = "");

/// <summary>
/// The only sanctioned way to create or change a <c>GuardianStudentRelationship</c> — build plan
/// §3: "never delete, close + reopen on change." A revision never overwrites the current row's
/// permission/authority fields in place; it closes the row (<c>EffectiveTo</c>) and opens a new
/// one, so the relationship's full history — including exactly what a guardian was and wasn't
/// permitted to see at any point in time — is always reconstructable.
/// </summary>
public interface IGuardianRelationshipService
{
    Task<Guid> EstablishAsync(EstablishGuardianRelationshipRequest request, CancellationToken cancellationToken = default);

    /// <summary>Closes <paramref name="currentRelationshipId"/> the day before <paramref name="effectiveFrom"/> and opens a new row with the revised values.</summary>
    Task<Guid> ReviseAsync(Guid currentRelationshipId, ReviseGuardianRelationshipRequest request, DateOnly effectiveFrom, CancellationToken cancellationToken = default);

    /// <summary>Ends the relationship entirely (e.g. legal custody revoked) — no replacement row.</summary>
    Task CloseAsync(Guid relationshipId, DateOnly effectiveTo, CancellationToken cancellationToken = default);

    /// <summary>
    /// The one sanctioned way to move a relationship from <c>Pending</c> to <c>Verified</c> (Phase
    /// 10 — this transition genuinely didn't exist anywhere before). Deliberately a separate,
    /// staff-gated administrative action, never an automatic side effect of a guardian's own OTP
    /// login: proving control of a phone number proves nothing about a claimed legal relationship —
    /// only a human reviewing real evidence (custody papers, an admission form) can verify that.
    /// </summary>
    /// <exception cref="InvalidOperationException">The relationship is not currently <c>Pending</c>.</exception>
    Task VerifyAsync(Guid relationshipId, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">The relationship is not currently <c>Pending</c>.</exception>
    Task RejectAsync(Guid relationshipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves which <c>Person</c> (if any) is a <c>Verified</c>, currently-active guardian
    /// reachable at <paramref name="phoneNumber"/> — the one lookup guardian OTP login needs
    /// (IdentityAccess, Phase 10). A phone number with no matching Verified relationship returns
    /// null; OTP login must never distinguish "no such phone number" from "not yet verified" in its
    /// own response (both fail the same way), but this method itself is honest about which case it hit.
    /// </summary>
    Task<Guid?> FindVerifiedGuardianPersonIdByPhoneAsync(string phoneNumber, DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>Every student this guardian has a currently-active, Verified relationship with, and exactly what they may see about each (the guardian portal's "which of my children" list, Phase 10).</summary>
    Task<IReadOnlyList<GuardianStudentSummary>> GetStudentsForGuardianAsync(Guid guardianPersonId, DateOnly asOf, CancellationToken cancellationToken = default);
}
