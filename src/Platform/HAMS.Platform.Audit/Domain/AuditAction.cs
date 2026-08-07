namespace HAMS.Platform.Audit.Domain;

/// <summary>
/// The fixed vocabulary of things worth auditing. A genuine C# enum by the same exception as
/// <c>RecordStatus</c> (Platform.Common) — this is a closed, code-emitted set that drives no
/// admin-facing configuration, not business/reference data.
/// </summary>
public enum AuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    Read = 3,
    Login = 4,
    LoginFailed = 5,
    Logout = 6,
    PermissionDenied = 7,
}
