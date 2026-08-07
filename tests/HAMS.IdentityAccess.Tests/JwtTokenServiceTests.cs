using System.IdentityModel.Tokens.Jwt;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.IdentityAccess.Domain;
using Microsoft.Extensions.Options;

namespace HAMS.IdentityAccess.Tests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() => new(Options.Create(new JwtOptions
    {
        Issuer = "HAMS.Tests",
        Audience = "HAMS.Tests.Clients",
        SigningKey = "unit-test-signing-key-at-least-32-bytes-long!!",
        AccessTokenLifetimeMinutes = 15,
        RefreshTokenLifetimeDays = 30,
    }));

    private static ApplicationUser CreateUser() => new() { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), UserName = "someone" };

    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    public void IssueAccessToken_sets_exactly_the_requested_principal_claims(bool isStaff, bool isGuardian, bool isStudent, bool isSystemAdmin)
    {
        var service = CreateService();
        var user = CreateUser();

        var (accessToken, _) = service.IssueAccessToken(user, isStaff, isGuardian, isStudent, isSystemAdmin);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Equal(user.PersonId.ToString(), jwt.Claims.Single(c => c.Type == HamsClaimTypes.PersonId).Value);
        Assert.Equal(isStaff ? "true" : "false", jwt.Claims.Single(c => c.Type == HamsClaimTypes.IsStaff).Value);
        Assert.Equal(isGuardian ? "true" : "false", jwt.Claims.Single(c => c.Type == HamsClaimTypes.IsGuardian).Value);
        Assert.Equal(isStudent ? "true" : "false", jwt.Claims.Single(c => c.Type == HamsClaimTypes.IsStudent).Value);
        Assert.Equal(isSystemAdmin ? "true" : "false", jwt.Claims.Single(c => c.Type == HamsClaimTypes.IsSystemAdmin).Value);
    }

    [Fact]
    public void HashRefreshToken_is_deterministic_and_never_reveals_the_original_token()
    {
        var service = CreateService();
        var token = service.GenerateRefreshToken();

        var hash1 = service.HashRefreshToken(token);
        var hash2 = service.HashRefreshToken(token);

        Assert.Equal(hash1, hash2);
        Assert.DoesNotContain(token, hash1);
    }
}
