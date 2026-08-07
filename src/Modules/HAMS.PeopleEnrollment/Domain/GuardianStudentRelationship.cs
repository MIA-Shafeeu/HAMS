using HAMS.Platform.Common.Contracts;

namespace HAMS.PeopleEnrollment.Domain;

/// <summary>
/// The single authoritative gate for guardian portal access, once that exists (build plan §3) —
/// relationship type, legal authority, information-access permissions, verification status, and
/// any restriction. Effective-dated, and by design never deleted or edited in place: a change
/// closes the current row (<see cref="IEffectiveDated.EffectiveTo"/>) and opens a new one via
/// <see cref="IGuardianRelationshipService.ReviseAsync"/>, so a guardian's access history is
/// always reconstructable — see that service's remarks for why.
/// </summary>
public sealed class GuardianStudentRelationship : IEffectiveDated
{
    public Guid Id { get; init; }

    public Guid GuardianPersonId { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid RelationshipTypeId { get; init; }

    public bool HasLegalAuthority { get; init; }

    /// <summary>What this guardian may be shown about the student, once guardian portal access exists.</summary>
    public bool CanViewAcademicRecords { get; init; }
    public bool CanViewAttendance { get; init; }
    public bool CanViewBehaviourRecords { get; init; }

    /// <summary>
    /// Gates the Intervention module's guardian-facing surface (Phase 10) — deliberately its own
    /// flag, not folded into <see cref="CanViewBehaviourRecords"/>: an intervention case is often
    /// purely academic remediation (e.g. additional Mastery-model practice) with nothing
    /// behavioural about it, and Phase 13's real Behaviour/Pastoral records don't exist yet, so
    /// reusing that flag now would risk a naming collision once they do.
    /// </summary>
    public bool CanViewInterventionUpdates { get; init; }

    public bool CanReceiveNotifications { get; init; }

    public GuardianVerificationStatus VerificationStatus { get; set; } = GuardianVerificationStatus.Pending;

    /// <summary>Null = no restriction in force.</summary>
    public Guid? RestrictionTypeId { get; init; }

    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; set; }
}
