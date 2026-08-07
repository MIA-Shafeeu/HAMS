using System.Security.Cryptography;
using System.Text;
using HAMS.IdentityAccess.Application.Auth;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.IdentityAccess.Domain;
using HAMS.IdentityAccess.Infrastructure;
using HAMS.Platform.Audit.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HAMS.IdentityAccess.Tests;

public class StaffAuthenticationServiceTests
{
    private const string Password = "Correct-Horse-1!";

    private sealed record Harness(
        StaffAuthenticationService Service, UserManager<ApplicationUser> UserManager, IdentityAccessDbContext DbContext, FakeAuditLogWriter Audit,
        FakeClock Clock, JwtTokenService JwtTokenService);

    private static Harness CreateHarness(DateOnly? today = null)
    {
        var (userManager, _, dbContext) = IdentityTestHarness.Create();
        var clock = new FakeClock(today ?? new DateOnly(2026, 8, 5));
        var jwtTokenService = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "HAMS.Tests", Audience = "HAMS.Tests.Clients", SigningKey = "unit-test-signing-key-at-least-32-bytes-long!!",
            AccessTokenLifetimeMinutes = 15, RefreshTokenLifetimeDays = 30,
        }));
        var audit = new FakeAuditLogWriter();
        var tokenIssuer = new TokenIssuer(
            dbContext, jwtTokenService, new FakeRoleMembershipQuery(), audit, clock,
            Options.Create(new JwtOptions { Issuer = "x", Audience = "x", SigningKey = "unit-test-signing-key-at-least-32-bytes-long!!", RefreshTokenLifetimeDays = 30 }));

        var service = new StaffAuthenticationService(userManager, dbContext, jwtTokenService, tokenIssuer, audit, clock);

        return new Harness(service, userManager, dbContext, audit, clock, jwtTokenService);
    }

    private static async Task<ApplicationUser> CreateActiveStaffUserAsync(UserManager<ApplicationUser> userManager, string username)
    {
        var user = new ApplicationUser { UserName = username, Email = $"{username}@hams.local", PersonId = Guid.NewGuid(), Status = AccountStatus.Active };
        var result = await userManager.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return user;
    }

    private static async Task EnableMfaAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        await userManager.ResetAuthenticatorKeyAsync(user);
        await userManager.SetTwoFactorEnabledAsync(user, true);
    }

    /// <summary>
    /// ASP.NET Core Identity's "Authenticator" token provider deliberately never generates a code
    /// itself (<c>UserManager.GenerateTwoFactorTokenAsync(user, "Authenticator")</c> always returns
    /// null) — a real TOTP code is supposed to come from an external authenticator app holding the
    /// same shared secret, never from the server. To get a code a test can actually submit, compute
    /// it here using the same RFC 6238 algorithm Identity's verifier uses internally: HMAC-SHA1 over
    /// the current 30-second Unix time step, RFC 4226 dynamic truncation, mod 10^6.
    /// </summary>
    private static async Task<string> GenerateValidTotpCodeAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        var base32Secret = await userManager.GetAuthenticatorKeyAsync(user) ?? throw new InvalidOperationException("No authenticator key set.");
        var key = Base32Decode(base32Secret);

        var timestepNumber = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var counter = new byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counter[i] = (byte)(timestepNumber & 0xff);
            timestepNumber >>= 8;
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0xf;
        var binaryCode = (hash[offset] & 0x7f) << 24
            | (hash[offset + 1] & 0xff) << 16
            | (hash[offset + 2] & 0xff) << 8
            | (hash[offset + 3] & 0xff);
        return (binaryCode % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        base32 = base32.TrimEnd('=').ToUpperInvariant();
        var bits = new StringBuilder();
        foreach (var c in base32)
        {
            var index = alphabet.IndexOf(c);
            if (index < 0) continue;
            bits.Append(Convert.ToString(index, 2).PadLeft(5, '0'));
        }

        var bytes = new List<byte>();
        for (var i = 0; i + 8 <= bits.Length; i += 8)
        {
            bytes.Add(Convert.ToByte(bits.ToString(i, 8), 2));
        }

        return bytes.ToArray();
    }

    [Fact]
    public async Task LoginAsync_with_correct_password_and_no_MFA_issues_tokens_directly()
    {
        var harness = CreateHarness();
        var user = await CreateActiveStaffUserAsync(harness.UserManager, "teacher1");

        var result = await harness.Service.LoginAsync(new StaffLoginRequest("teacher1", Password, null), "127.0.0.1");

        Assert.True(result.Succeeded);
        Assert.False(result.MfaRequired);
        Assert.NotNull(result.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_with_MFA_enabled_requires_a_second_step_instead_of_issuing_tokens()
    {
        var harness = CreateHarness();
        var user = await CreateActiveStaffUserAsync(harness.UserManager, "teacher2");
        await EnableMfaAsync(harness.UserManager, user);

        var result = await harness.Service.LoginAsync(new StaffLoginRequest("teacher2", Password, null), "127.0.0.1");

        Assert.False(result.Succeeded);
        Assert.True(result.MfaRequired);
        Assert.NotNull(result.MfaToken);
        Assert.Null(result.AccessToken);
    }

    [Fact]
    public async Task VerifyMfaAsync_with_the_correct_code_issues_tokens()
    {
        var harness = CreateHarness();
        var user = await CreateActiveStaffUserAsync(harness.UserManager, "teacher3");
        await EnableMfaAsync(harness.UserManager, user);
        var loginResult = await harness.Service.LoginAsync(new StaffLoginRequest("teacher3", Password, null), "127.0.0.1");
        var code = await GenerateValidTotpCodeAsync(harness.UserManager, user);

        var result = await harness.Service.VerifyMfaAsync(new StaffMfaVerifyRequest(loginResult.MfaToken!, code, null), "127.0.0.1");

        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.AccessToken);
    }

    [Fact]
    public async Task VerifyMfaAsync_with_the_wrong_code_fails_and_counts_the_attempt()
    {
        var harness = CreateHarness();
        var user = await CreateActiveStaffUserAsync(harness.UserManager, "teacher4");
        await EnableMfaAsync(harness.UserManager, user);
        var loginResult = await harness.Service.LoginAsync(new StaffLoginRequest("teacher4", Password, null), "127.0.0.1");

        var result = await harness.Service.VerifyMfaAsync(new StaffMfaVerifyRequest(loginResult.MfaToken!, "000000", null), "127.0.0.1");

        Assert.False(result.Succeeded);
        var reloaded = await harness.UserManager.FindByNameAsync("teacher4");
        Assert.Equal(1, await harness.UserManager.GetAccessFailedCountAsync(reloaded!));
    }

    /// <summary>
    /// Regression guard for the fix: MFA verification previously had no lockout of its own, so a
    /// stolen/guessed password let an attacker brute-force the 6-digit TOTP code with unlimited
    /// attempts across as many freshly-issued challenge tokens as they liked. This proves the same
    /// 5-attempt lockout the password step already enforces now also applies here — and that a
    /// correct password on a subsequent LoginAsync call can't reset the counter mid-MFA (otherwise
    /// an attacker who knows/guesses the password defeats the lockout for free by just calling
    /// LoginAsync again between guesses). Once locked, LoginAsync itself refuses to issue a fresh
    /// MFA challenge at all — there's nothing left to submit a code against.
    /// </summary>
    [Fact]
    public async Task VerifyMfaAsync_locks_out_after_five_wrong_attempts_and_a_correct_password_cannot_reset_it()
    {
        var harness = CreateHarness();
        var user = await CreateActiveStaffUserAsync(harness.UserManager, "teacher5");
        await EnableMfaAsync(harness.UserManager, user);

        for (var i = 0; i < 5; i++)
        {
            var loginResult = await harness.Service.LoginAsync(new StaffLoginRequest("teacher5", Password, null), "127.0.0.1");
            await harness.Service.VerifyMfaAsync(new StaffMfaVerifyRequest(loginResult.MfaToken!, "000000", null), "127.0.0.1");
        }

        var finalLogin = await harness.Service.LoginAsync(new StaffLoginRequest("teacher5", Password, null), "127.0.0.1");

        Assert.False(finalLogin.Succeeded);
        Assert.False(finalLogin.MfaRequired);
        Assert.Null(finalLogin.MfaToken);
        Assert.Equal("This account is temporarily locked due to repeated failed sign-in attempts.", finalLogin.Error);
    }

    [Fact]
    public async Task LogoutAsync_revokes_the_session_and_writes_an_audit_entry()
    {
        var harness = CreateHarness();
        var user = await CreateActiveStaffUserAsync(harness.UserManager, "teacher6");
        var loginResult = await harness.Service.LoginAsync(new StaffLoginRequest("teacher6", Password, null), "127.0.0.1");

        await harness.Service.LogoutAsync(loginResult.RefreshToken!);

        var session = await harness.DbContext.UserSessions.SingleAsync(s => s.UserId == user.Id);
        Assert.NotNull(session.RevokedAtUtc);
        Assert.Contains(harness.Audit.Entries, e => e.Action == AuditAction.Logout && e.ActorUserId == user.Id);
    }

    [Fact]
    public async Task LogoutAsync_is_a_noop_for_an_unknown_refresh_token()
    {
        var harness = CreateHarness();

        await harness.Service.LogoutAsync("not-a-real-refresh-token");

        Assert.DoesNotContain(harness.Audit.Entries, e => e.Action == AuditAction.Logout);
    }

    [Fact]
    public async Task RefreshAsync_preserves_the_staff_flag_from_the_original_login()
    {
        var harness = CreateHarness();
        var user = await CreateActiveStaffUserAsync(harness.UserManager, "teacher7");
        var loginResult = await harness.Service.LoginAsync(new StaffLoginRequest("teacher7", Password, null), "127.0.0.1");

        var refreshResult = await harness.Service.RefreshAsync(new RefreshRequest(loginResult.RefreshToken!), "127.0.0.1");

        Assert.True(refreshResult.Succeeded);
        AssertTokenClaim(refreshResult.AccessToken!, HamsClaimTypes.IsStaff, "true");
        AssertTokenClaim(refreshResult.AccessToken!, HamsClaimTypes.IsGuardian, "false");
        AssertTokenClaim(refreshResult.AccessToken!, HamsClaimTypes.IsStudent, "false");
    }

    /// <summary>
    /// Regression test for a real bug: <c>RefreshAsync</c> (the one generic implementation every
    /// principal type's <c>/api/v1/auth/refresh</c> funnels through) used to hardcode
    /// <c>isStaff: true, isGuardian: false, isStudent: false</c> regardless of which login path
    /// actually created the session — so refreshing a guardian's or student's token silently came
    /// back staff-flagged. Fixed by persisting the principal type on <see cref="UserSession"/> at
    /// issuance and reading it back here. This seeds a guardian-flagged session directly (bypassing
    /// <c>GuardianAuthenticationService</c>, which isn't under test here) since <c>RefreshAsync</c>
    /// itself is agnostic to which service originally issued the session.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_preserves_the_guardian_flag_from_the_original_login()
    {
        var harness = CreateHarness();
        var user = await CreateActiveStaffUserAsync(harness.UserManager, "guardian1");
        const string rawRefreshToken = "guardian-session-refresh-token";
        harness.DbContext.UserSessions.Add(new UserSession
        {
            Id = Guid.NewGuid(), UserId = user.Id, IsStaff = false, IsGuardian = true, IsStudent = false,
            RefreshTokenHash = harness.JwtTokenService.HashRefreshToken(rawRefreshToken),
            CreatedAtUtc = harness.Clock.UtcNow, ExpiresAtUtc = harness.Clock.UtcNow.AddDays(30),
        });
        await harness.DbContext.SaveChangesAsync();

        var refreshResult = await harness.Service.RefreshAsync(new RefreshRequest(rawRefreshToken), "127.0.0.1");

        Assert.True(refreshResult.Succeeded, refreshResult.Error);
        AssertTokenClaim(refreshResult.AccessToken!, HamsClaimTypes.IsStaff, "false");
        AssertTokenClaim(refreshResult.AccessToken!, HamsClaimTypes.IsGuardian, "true");
        AssertTokenClaim(refreshResult.AccessToken!, HamsClaimTypes.IsStudent, "false");
    }

    [Fact]
    public async Task RefreshAsync_preserves_the_student_flag_from_the_original_login()
    {
        var harness = CreateHarness();
        var user = await CreateActiveStaffUserAsync(harness.UserManager, "student1");
        const string rawRefreshToken = "student-session-refresh-token";
        harness.DbContext.UserSessions.Add(new UserSession
        {
            Id = Guid.NewGuid(), UserId = user.Id, IsStaff = false, IsGuardian = false, IsStudent = true,
            RefreshTokenHash = harness.JwtTokenService.HashRefreshToken(rawRefreshToken),
            CreatedAtUtc = harness.Clock.UtcNow, ExpiresAtUtc = harness.Clock.UtcNow.AddDays(30),
        });
        await harness.DbContext.SaveChangesAsync();

        var refreshResult = await harness.Service.RefreshAsync(new RefreshRequest(rawRefreshToken), "127.0.0.1");

        Assert.True(refreshResult.Succeeded, refreshResult.Error);
        AssertTokenClaim(refreshResult.AccessToken!, HamsClaimTypes.IsStaff, "false");
        AssertTokenClaim(refreshResult.AccessToken!, HamsClaimTypes.IsGuardian, "false");
        AssertTokenClaim(refreshResult.AccessToken!, HamsClaimTypes.IsStudent, "true");
    }

    private static void AssertTokenClaim(string accessToken, string claimType, string expectedValue)
    {
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var actual = token.Claims.SingleOrDefault(c => c.Type == claimType)?.Value;
        Assert.Equal(expectedValue, actual);
    }
}
