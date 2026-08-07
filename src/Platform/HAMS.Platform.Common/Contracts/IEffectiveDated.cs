namespace HAMS.Platform.Common.Contracts;

/// <summary>
/// Shared shape for anything that represents an assignment or relationship in force over time —
/// teaching assignments, class-teacher/leading-teacher assignments, guardian-student
/// relationships, student enrolments, key-stage/grade assignments, role assignments, and so on.
///
/// This is the mechanism the Access-Scope permission kernel (Platform.Access) relies on for
/// automatic expiry: because every permission check re-queries live effective-dated data via
/// <see cref="EffectiveDatedQueryExtensions.ActiveAsOf{T}"/>, access disappears the instant
/// <see cref="EffectiveTo"/> passes with no scheduled revocation job required for correctness.
/// </summary>
public interface IEffectiveDated
{
    DateOnly EffectiveFrom { get; }

    /// <summary>Null means "still in force, no end date set."</summary>
    DateOnly? EffectiveTo { get; }
}
