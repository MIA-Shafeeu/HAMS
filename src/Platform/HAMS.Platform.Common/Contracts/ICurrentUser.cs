namespace HAMS.Platform.Common.Contracts;

/// <summary>
/// What the JWT/cookie claims actually carry — deliberately minimal, per the plan's access-scope
/// design: fine-grained scope (role, school, key stage, etc.) is never embedded in a token and is
/// always re-resolved per-request from live <c>AccessGrant</c> data, avoiding the classic
/// "stale JWT still grants revoked access" bug (IAM-FR-016). The coarse Is* flags below are only
/// enough to drive top-level UI shell decisions (which portal shell to render) — they are never
/// used for an actual authorization decision.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>Null when <see cref="IsAuthenticated"/> is false.</summary>
    Guid? UserId { get; }

    /// <summary>
    /// The underlying <c>Person</c> this account belongs to. A single person may simultaneously
    /// hold staff, guardian and/or student roles — <see cref="PersonId"/> is the stable identity
    /// that <c>AccessGrant</c> rows and every scoped-resource check key off.
    /// </summary>
    Guid? PersonId { get; }

    bool IsStaff { get; }
    bool IsGuardian { get; }
    bool IsStudent { get; }
    bool IsSystemAdmin { get; }
}
