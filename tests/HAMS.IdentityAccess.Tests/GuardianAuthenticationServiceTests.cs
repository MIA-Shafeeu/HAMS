using System.Text.RegularExpressions;
using HAMS.IdentityAccess.Application.Auth;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.IdentityAccess.Domain;
using HAMS.IdentityAccess.Infrastructure;
using HAMS.Platform.Access.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HAMS.IdentityAccess.Tests;

public class GuardianAuthenticationServiceTests
{
    private const string Phone = "+9609701776";

    private sealed record Harness(
        GuardianAuthenticationService Service, IdentityAccessDbContext DbContext, FakeSmsSender SmsSender,
        FakePersonRoleAssignmentService RoleAssignments, FakeClock Clock);

    private static Harness CreateHarness(Guid guardianPersonId, DateOnly? today = null, bool guardianRoleAlreadyHeld = false)
    {
        var (userManager, _, dbContext) = IdentityTestHarness.Create();
        var clock = new FakeClock(today ?? new DateOnly(2026, 8, 5));
        var smsSender = new FakeSmsSender();
        var roleAssignments = new FakePersonRoleAssignmentService();
        var jwtTokenService = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "HAMS.Tests", Audience = "HAMS.Tests.Clients", SigningKey = "unit-test-signing-key-at-least-32-bytes-long!!",
            AccessTokenLifetimeMinutes = 15, RefreshTokenLifetimeDays = 30,
        }));
        var tokenIssuer = new TokenIssuer(
            dbContext, jwtTokenService, new FakeRoleMembershipQuery(), new FakeAuditLogWriter(), clock,
            Options.Create(new JwtOptions { Issuer = "x", Audience = "x", SigningKey = "unit-test-signing-key-at-least-32-bytes-long!!", RefreshTokenLifetimeDays = 30 }));

        var guardianRelationshipService = new FakeGuardianRelationshipService(new Dictionary<string, Guid> { [Phone] = guardianPersonId });

        var service = new GuardianAuthenticationService(
            dbContext, userManager, guardianRelationshipService, roleAssignments, new FakeRoleMembershipQuery(guardianRoleAlreadyHeld), smsSender,
            tokenIssuer, new FakeAuditLogWriter(), clock);

        return new Harness(service, dbContext, smsSender, roleAssignments, clock);
    }

    private static string ExtractCode(string smsBody) => Regex.Match(smsBody, @"\d{6}").Value;

    [Fact]
    public async Task RequestOtpAsync_fails_for_a_phone_number_with_no_verified_guardian()
    {
        var harness = CreateHarness(Guid.NewGuid());

        var result = await harness.Service.RequestOtpAsync("+9609999999");

        Assert.False(result.Succeeded);
        Assert.Empty(harness.SmsSender.Sent);
    }

    [Fact]
    public async Task RequestOtpAsync_sends_a_code_and_creates_a_challenge()
    {
        var guardianId = Guid.NewGuid();
        var harness = CreateHarness(guardianId);

        var result = await harness.Service.RequestOtpAsync(Phone);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.ChallengeId);
        Assert.Single(harness.SmsSender.Sent);
        Assert.Equal(Phone, harness.SmsSender.Sent[0].PhoneNumber);

        var challenge = await harness.DbContext.GuardianOtpChallenges.SingleAsync(c => c.Id == result.ChallengeId);
        Assert.Equal(guardianId, challenge.PersonId);
        Assert.Null(challenge.ConsumedAtUtc);
    }

    [Fact]
    public async Task RequestOtpAsync_invalidates_a_still_outstanding_prior_code_for_the_same_number()
    {
        var harness = CreateHarness(Guid.NewGuid());

        var first = await harness.Service.RequestOtpAsync(Phone);
        await harness.Service.RequestOtpAsync(Phone);

        var firstChallenge = await harness.DbContext.GuardianOtpChallenges.SingleAsync(c => c.Id == first.ChallengeId);
        Assert.True(firstChallenge.ExpiresAtUtc <= harness.Clock.UtcNow);
    }

    [Fact]
    public async Task VerifyOtpAsync_with_the_correct_code_issues_tokens_and_provisions_the_guardian_role()
    {
        var guardianId = Guid.NewGuid();
        var harness = CreateHarness(guardianId);
        var requestResult = await harness.Service.RequestOtpAsync(Phone);
        var code = ExtractCode(harness.SmsSender.Sent[0].Message);

        var result = await harness.Service.VerifyOtpAsync(requestResult.ChallengeId!.Value, code, "unit-test-device", "127.0.0.1");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.Single(harness.RoleAssignments.Assignments, a => a.PersonId == guardianId && a.RoleCode == RoleCodes.Guardian);

        var user = await harness.DbContext.Users.SingleAsync(u => u.PersonId == guardianId);
        Assert.Equal(Phone, user.UserName);

        var challenge = await harness.DbContext.GuardianOtpChallenges.SingleAsync(c => c.Id == requestResult.ChallengeId);
        Assert.NotNull(challenge.ConsumedAtUtc);
    }

    [Fact]
    public async Task VerifyOtpAsync_does_not_reassign_the_Guardian_role_when_already_held()
    {
        var guardianId = Guid.NewGuid();
        var harness = CreateHarness(guardianId, guardianRoleAlreadyHeld: true);
        var requestResult = await harness.Service.RequestOtpAsync(Phone);

        await harness.Service.VerifyOtpAsync(requestResult.ChallengeId!.Value, ExtractCode(harness.SmsSender.Sent[0].Message), null, null);

        Assert.Empty(harness.RoleAssignments.Assignments);
    }

    [Fact]
    public async Task VerifyOtpAsync_reuses_the_same_ApplicationUser_across_repeated_logins()
    {
        var guardianId = Guid.NewGuid();
        var harness = CreateHarness(guardianId);
        var firstRequest = await harness.Service.RequestOtpAsync(Phone);
        await harness.Service.VerifyOtpAsync(firstRequest.ChallengeId!.Value, ExtractCode(harness.SmsSender.Sent[0].Message), null, null);

        var secondRequest = await harness.Service.RequestOtpAsync(Phone);
        await harness.Service.VerifyOtpAsync(secondRequest.ChallengeId!.Value, ExtractCode(harness.SmsSender.Sent[1].Message), null, null);

        var userCount = await harness.DbContext.Users.CountAsync(u => u.PersonId == guardianId);
        Assert.Equal(1, userCount);
    }

    [Fact]
    public async Task VerifyOtpAsync_with_the_wrong_code_fails_and_counts_the_attempt()
    {
        var harness = CreateHarness(Guid.NewGuid());
        var requestResult = await harness.Service.RequestOtpAsync(Phone);

        var result = await harness.Service.VerifyOtpAsync(requestResult.ChallengeId!.Value, "000000", null, null);

        Assert.False(result.Succeeded);
        var challenge = await harness.DbContext.GuardianOtpChallenges.SingleAsync(c => c.Id == requestResult.ChallengeId);
        Assert.Equal(1, challenge.AttemptCount);
        Assert.Null(challenge.ConsumedAtUtc);
    }

    [Fact]
    public async Task VerifyOtpAsync_fails_after_five_wrong_attempts_even_with_the_right_code_afterward()
    {
        var harness = CreateHarness(Guid.NewGuid());
        var requestResult = await harness.Service.RequestOtpAsync(Phone);
        var correctCode = ExtractCode(harness.SmsSender.Sent[0].Message);

        for (var i = 0; i < 5; i++)
        {
            await harness.Service.VerifyOtpAsync(requestResult.ChallengeId!.Value, "000000", null, null);
        }

        var result = await harness.Service.VerifyOtpAsync(requestResult.ChallengeId!.Value, correctCode, null, null);

        Assert.False(result.Succeeded);
        Assert.Equal("Too many attempts. Request a new code.", result.Error);
    }

    [Fact]
    public async Task VerifyOtpAsync_rejects_an_already_consumed_challenge()
    {
        var harness = CreateHarness(Guid.NewGuid());
        var requestResult = await harness.Service.RequestOtpAsync(Phone);
        var code = ExtractCode(harness.SmsSender.Sent[0].Message);
        await harness.Service.VerifyOtpAsync(requestResult.ChallengeId!.Value, code, null, null);

        var result = await harness.Service.VerifyOtpAsync(requestResult.ChallengeId!.Value, code, null, null);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task VerifyOtpAsync_rejects_an_expired_challenge()
    {
        var harness = CreateHarness(Guid.NewGuid(), today: new DateOnly(2026, 8, 5));
        var requestResult = await harness.Service.RequestOtpAsync(Phone);
        var code = ExtractCode(harness.SmsSender.Sent[0].Message);

        var challenge = await harness.DbContext.GuardianOtpChallenges.SingleAsync(c => c.Id == requestResult.ChallengeId);
        challenge.ExpiresAtUtc = harness.Clock.UtcNow.AddMinutes(-1);
        await harness.DbContext.SaveChangesAsync();

        var result = await harness.Service.VerifyOtpAsync(requestResult.ChallengeId!.Value, code, null, null);

        Assert.False(result.Succeeded);
    }
}
