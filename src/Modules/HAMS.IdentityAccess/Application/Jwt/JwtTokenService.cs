using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HAMS.IdentityAccess.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HAMS.IdentityAccess.Application.Jwt;

public interface IJwtTokenService
{
    /// <summary>
    /// Issues the one access-token shape every principal type converges on (build plan §5) — which
    /// of <paramref name="isStaff"/>/<paramref name="isGuardian"/>/<paramref name="isStudent"/> is
    /// true depends only on which login path called this, never on inspecting <paramref name="user"/>
    /// itself (a guardian/student <see cref="ApplicationUser"/> row looks structurally identical to
    /// a staff one). <paramref name="isSystemAdmin"/> is meaningless for non-staff callers and
    /// should always be passed <see langword="false"/> for them.
    /// </summary>
    (string AccessToken, DateTimeOffset ExpiresAtUtc) IssueAccessToken(
        ApplicationUser user, bool isStaff, bool isGuardian, bool isStudent, bool isSystemAdmin);

    /// <summary>A short-lived (5 min), self-contained token identifying the user between the password
    /// and TOTP steps of login — avoids needing extra server-side storage for the pending challenge.</summary>
    string IssueMfaChallengeToken(Guid userId);

    bool TryValidateMfaChallengeToken(string token, out Guid userId);

    /// <summary>High-entropy opaque secret — only its hash is ever persisted (see <see cref="HashRefreshToken"/>).</summary>
    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);
}

internal sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private const string MfaChallengeTokenType = "mfa_challenge";

    private readonly JwtOptions _options = options.Value;

    public (string AccessToken, DateTimeOffset ExpiresAtUtc) IssueAccessToken(
        ApplicationUser user, bool isStaff, bool isGuardian, bool isStudent, bool isSystemAdmin)
    {
        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(HamsClaimTypes.PersonId, user.PersonId.ToString()),
            new(HamsClaimTypes.IsStaff, isStaff ? "true" : "false"),
            new(HamsClaimTypes.IsGuardian, isGuardian ? "true" : "false"),
            new(HamsClaimTypes.IsStudent, isStudent ? "true" : "false"),
            new(HamsClaimTypes.IsSystemAdmin, isSystemAdmin ? "true" : "false"),
        ];

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: SigningCredentials());

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public string IssueMfaChallengeToken(Guid userId)
    {
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("typ", MfaChallengeTokenType),
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: SigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool TryValidateMfaChallengeToken(string token, out Guid userId)
    {
        userId = default;

        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = SigningKey(),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);

            if (principal.FindFirstValue("typ") != MfaChallengeTokenType)
            {
                return false;
            }

            // Not FindFirstValue(JwtRegisteredClaimNames.Sub): JwtSecurityTokenHandler's default inbound
            // claim map rewrites the "sub" claim to ClaimTypes.NameIdentifier during ValidateToken, so the
            // literal "sub" claim type never survives on the resulting principal (same reason IssueAccessToken
            // writes ClaimTypes.NameIdentifier explicitly alongside Sub).
            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out userId);
        }
        catch (SecurityTokenException)
        {
            return false;
        }
    }

    public string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(_options.SigningKey));

    private SigningCredentials SigningCredentials() => new(SigningKey(), SecurityAlgorithms.HmacSha256);
}
