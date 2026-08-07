namespace HAMS.IdentityAccess.Application.Jwt;

/// <summary>Bound from the "Jwt" configuration section — see appsettings.Development.json for local dev values.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; set; }

    public required string Audience { get; set; }

    /// <summary>Symmetric signing key. Production deployments must override this via environment/secret configuration, never commit it.</summary>
    public required string SigningKey { get; set; }

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
