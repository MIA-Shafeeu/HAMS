namespace HAMS.IdentityAccess.Domain;

/// <summary>
/// Admin-driven account lifecycle (IAM-FR: "administrators shall be able to disable, suspend,
/// reactivate... accounts without deleting historical activity"). A genuine C# enum by the same
/// exception as <c>RecordStatus</c> (Platform.Common) — this is a fixed, structural state, not
/// business/reference data a school renames. Deliberately distinct from ASP.NET Core Identity's
/// own <c>LockoutEnd</c>, which throttles failed-login attempts rather than reflecting an
/// administrator's decision.
/// </summary>
public enum AccountStatus
{
    Active = 0,
    Suspended = 1,
    Disabled = 2,
}
