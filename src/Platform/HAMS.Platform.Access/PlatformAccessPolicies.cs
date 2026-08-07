namespace HAMS.Platform.Access;

/// <summary>ASP.NET Core authorization policy names registered by <see cref="PlatformAccessExtensions.AddPlatformAccess"/>.</summary>
public static class PlatformAccessPolicies
{
    /// <summary>Resource-based policy backed by <c>ScopeAuthorizationHandler</c>. Call via <c>IAuthorizationService.AuthorizeAsync(user, resource, Scope)</c>.</summary>
    public const string Scope = "HAMS.Scope";

    /// <summary>Resource-based policy backed by <c>ConfidentialityAuthorizationHandler</c>. Always AND-ed alongside <see cref="Scope"/>, never used alone.</summary>
    public const string Confidentiality = "HAMS.Confidentiality";
}
