namespace HAMS.IdentityAccess.Application.Jwt;

/// <summary>
/// The deliberately minimal claim set embedded in every access token (build plan §4: "Claims stay
/// minimal... fine-grained scope is never embedded in a token"). <see cref="IsSystemAdmin"/> and
/// friends are coarse, UI-shell-only flags recomputed at every token issuance (so staleness is
/// bounded by the access token's short lifetime) — never used for an actual authorization decision.
/// </summary>
public static class HamsClaimTypes
{
    public const string PersonId = "hams:person_id";
    public const string IsStaff = "hams:is_staff";
    public const string IsGuardian = "hams:is_guardian";
    public const string IsStudent = "hams:is_student";
    public const string IsSystemAdmin = "hams:is_system_admin";
}
