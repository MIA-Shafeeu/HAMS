using HAMS.IdentityAccess.Application.Auth;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.IdentityAccess.Domain;
using HAMS.IdentityAccess.Infrastructure;
using HAMS.Platform.Access.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HAMS.IdentityAccess.Tests;

public class StudentAuthenticationServiceTests
{
    private const string AdmissionNumber = "ADM-2026-0042";

    private sealed record Harness(StudentAuthenticationService Service, IdentityAccessDbContext DbContext, FakePersonRoleAssignmentService RoleAssignments);

    private static Harness CreateHarness(Guid studentPersonId, bool studentRoleAlreadyHeld = false)
    {
        var (userManager, passwordHasher, dbContext) = IdentityTestHarness.Create();
        var clock = new FakeClock(new DateOnly(2026, 8, 5));
        var roleAssignments = new FakePersonRoleAssignmentService();
        var jwtTokenService = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "HAMS.Tests", Audience = "HAMS.Tests.Clients", SigningKey = "unit-test-signing-key-at-least-32-bytes-long!!",
            AccessTokenLifetimeMinutes = 15, RefreshTokenLifetimeDays = 30,
        }));
        var tokenIssuer = new TokenIssuer(
            dbContext, jwtTokenService, new FakeRoleMembershipQuery(), new FakeAuditLogWriter(), clock,
            Options.Create(new JwtOptions { Issuer = "x", Audience = "x", SigningKey = "unit-test-signing-key-at-least-32-bytes-long!!", RefreshTokenLifetimeDays = 30 }));

        var profileLookup = new FakeStudentProfileLookup(new Dictionary<string, Guid> { [AdmissionNumber] = studentPersonId });

        var service = new StudentAuthenticationService(
            dbContext, userManager, passwordHasher, profileLookup, roleAssignments, new FakeRoleMembershipQuery(studentRoleAlreadyHeld),
            tokenIssuer, new FakeAuditLogWriter(), clock);

        return new Harness(service, dbContext, roleAssignments);
    }

    [Fact]
    public async Task LoginAsync_fails_for_an_unknown_admission_number()
    {
        var harness = CreateHarness(Guid.NewGuid());

        var result = await harness.Service.LoginAsync(new StudentLoginRequest("NO-SUCH-ID", "1234", null), null);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task LoginAsync_fails_when_no_PIN_has_ever_been_set()
    {
        var harness = CreateHarness(Guid.NewGuid());

        var result = await harness.Service.LoginAsync(new StudentLoginRequest(AdmissionNumber, "1234", null), null);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SetPinAsync_then_LoginAsync_with_the_correct_PIN_succeeds_and_provisions_the_Student_role()
    {
        var studentId = Guid.NewGuid();
        var harness = CreateHarness(studentId);

        await harness.Service.SetPinAsync(studentId, "1234");
        var result = await harness.Service.LoginAsync(new StudentLoginRequest(AdmissionNumber, "1234", null), null);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.AccessToken);
        Assert.Single(harness.RoleAssignments.Assignments, a => a.PersonId == studentId && a.RoleCode == RoleCodes.Student);
    }

    [Fact]
    public async Task SetPinAsync_does_not_reassign_the_Student_role_when_already_held()
    {
        var studentId = Guid.NewGuid();
        var harness = CreateHarness(studentId, studentRoleAlreadyHeld: true);

        await harness.Service.SetPinAsync(studentId, "1234");

        Assert.Empty(harness.RoleAssignments.Assignments);
    }

    [Fact]
    public async Task LoginAsync_fails_with_the_wrong_PIN()
    {
        var studentId = Guid.NewGuid();
        var harness = CreateHarness(studentId);
        await harness.Service.SetPinAsync(studentId, "1234");

        var result = await harness.Service.LoginAsync(new StudentLoginRequest(AdmissionNumber, "9999", null), null);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SetPinAsync_a_second_time_replaces_the_old_PIN_entirely()
    {
        var studentId = Guid.NewGuid();
        var harness = CreateHarness(studentId);
        await harness.Service.SetPinAsync(studentId, "1234");

        await harness.Service.SetPinAsync(studentId, "5678");

        var oldPinResult = await harness.Service.LoginAsync(new StudentLoginRequest(AdmissionNumber, "1234", null), null);
        var newPinResult = await harness.Service.LoginAsync(new StudentLoginRequest(AdmissionNumber, "5678", null), null);

        Assert.False(oldPinResult.Succeeded);
        Assert.True(newPinResult.Succeeded);

        var userCount = await harness.DbContext.Users.CountAsync(u => u.PersonId == studentId);
        Assert.Equal(1, userCount);
    }

    [Fact]
    public async Task LoginAsync_locks_the_account_out_after_five_failed_attempts()
    {
        var studentId = Guid.NewGuid();
        var harness = CreateHarness(studentId);
        await harness.Service.SetPinAsync(studentId, "1234");

        for (var i = 0; i < 5; i++)
        {
            await harness.Service.LoginAsync(new StudentLoginRequest(AdmissionNumber, "wrong", null), null);
        }

        var result = await harness.Service.LoginAsync(new StudentLoginRequest(AdmissionNumber, "1234", null), null);

        Assert.False(result.Succeeded);
        Assert.Contains("locked", result.Error);
    }
}
