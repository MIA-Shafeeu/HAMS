using HAMS.IdentityAccess.Domain;
using HAMS.IdentityAccess.Infrastructure;
using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Domain;
using HAMS.Platform.Access;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Notifications.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.IdentityAccess.Tests;

internal sealed class FakeClock(DateOnly today) : IClock
{
    public DateTimeOffset UtcNow => today.ToDateTime(TimeOnly.MinValue);
    public DateOnly TodayUtc => today;
}

/// <summary>Always reports no roles held — sufficient for guardian/student login paths, which only ever check/assign their own single role.</summary>
internal sealed class FakeRoleMembershipQuery(bool hasRole = false) : IRoleMembershipQuery
{
    public Task<bool> HasRoleAsync(Guid personId, string roleCode, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult(hasRole);

    public Task<bool> HasAnyRoleAsync(Guid personId, IReadOnlyCollection<string> roleCodes, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult(hasRole);

    public Task<bool> AnyPersonHasRoleAsync(string roleCode, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult(hasRole);
}

/// <summary>
/// Also implements <see cref="IRoleMembershipQuery"/> against its own <see cref="Assignments"/> list
/// — pass the SAME instance for both constructor parameters where a test needs "assign a role, then
/// see the assignment reflected in a membership check" (e.g. <c>SetupServiceTests</c>'s "bootstrapping
/// twice refuses the second time"), which two independently-configured fakes can't give you.
/// </summary>
internal sealed class FakePersonRoleAssignmentService : IPersonRoleAssignmentService, IRoleMembershipQuery
{
    public List<(Guid PersonId, string RoleCode)> Assignments { get; } = [];

    public Task<Guid> AssignRoleAsync(Guid personId, string roleCode, Guid? schoolId, DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default)
    {
        Assignments.Add((personId, roleCode));
        return Task.FromResult(Guid.NewGuid());
    }

    public Task<bool> HasRoleAsync(Guid personId, string roleCode, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult(Assignments.Contains((personId, roleCode)));

    public Task<bool> HasAnyRoleAsync(Guid personId, IReadOnlyCollection<string> roleCodes, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult(Assignments.Any(a => a.PersonId == personId && roleCodes.Contains(a.RoleCode)));

    public Task<bool> AnyPersonHasRoleAsync(string roleCode, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult(Assignments.Any(a => a.RoleCode == roleCode));

    public Task RevokeRoleAsync(Guid personRoleAssignmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task<IReadOnlyList<HAMS.Platform.Access.Domain.Role>> GetRolesAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task<IReadOnlyList<HAMS.Platform.Access.Domain.Role>> GetAllRolesAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task<IReadOnlyList<HAMS.Platform.Access.Domain.PersonRoleAssignment>> GetAssignmentsForPersonAsync(Guid personId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task<Guid> CreateRoleAsync(string code, string name, string? description, int displayOrder, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task SetRoleActiveAsync(Guid roleId, bool isActive, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task UpdateRoleAsync(Guid roleId, string name, int displayOrder, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task<Guid> CreateConfidentialityTierAsync(string code, string name, string? description, int rank, int displayOrder, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task<IReadOnlyList<HAMS.Platform.Access.Domain.ConfidentialityTier>> GetConfidentialityTiersAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task SetConfidentialityTierActiveAsync(Guid tierId, bool isActive, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task UpdateConfidentialityTierAsync(Guid tierId, string name, int rank, int displayOrder, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");
}

internal sealed class FakeAuditLogWriter : IAuditLogWriter
{
    public List<AuditLogEntry> Entries { get; } = [];

    public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}

internal sealed class FakeSmsSender : ISmsSender
{
    public List<(string PhoneNumber, string Message)> Sent { get; } = [];

    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        Sent.Add((phoneNumber, message));
        return Task.CompletedTask;
    }
}

/// <summary>Keyed by phone number — configure with whichever (phoneNumber, guardianPersonId) pairs a test needs to resolve as Verified.</summary>
internal sealed class FakeGuardianRelationshipService(IReadOnlyDictionary<string, Guid> verifiedGuardiansByPhone) : IGuardianRelationshipService
{
    public Task<Guid> EstablishAsync(EstablishGuardianRelationshipRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task<Guid> ReviseAsync(Guid currentRelationshipId, ReviseGuardianRelationshipRequest request, DateOnly effectiveFrom, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task CloseAsync(Guid relationshipId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task VerifyAsync(Guid relationshipId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task RejectAsync(Guid relationshipId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");

    public Task<Guid?> FindVerifiedGuardianPersonIdByPhoneAsync(string phoneNumber, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult(verifiedGuardiansByPhone.TryGetValue(phoneNumber, out var personId) ? personId : (Guid?)null);

    public Task<IReadOnlyList<GuardianStudentSummary>> GetStudentsForGuardianAsync(Guid guardianPersonId, DateOnly asOf, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by auth tests.");
}

/// <summary>Keyed by admission number.</summary>
internal sealed class FakeStudentProfileLookup(IReadOnlyDictionary<string, Guid> personIdsByAdmissionNumber) : IStudentProfileLookup
{
    public Task<Guid?> FindPersonIdByAdmissionNumberAsync(string admissionNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(personIdsByAdmissionNumber.TryGetValue(admissionNumber, out var personId) ? personId : (Guid?)null);
}

/// <summary>
/// A real, minimal ASP.NET Core Identity DI stack (same registration shape as
/// <c>IdentityAccessModule.AddIdentityAccessModule</c>) against EF Core InMemory — Identity's basic
/// Create/Find/CheckPassword/lockout mechanics are plain CRUD with no relational-specific SQL
/// features, so InMemory is sufficient here (contrast with Phase 3/4's filtered-index/cross-context-
/// transaction features, which genuinely need a real SQL Server provider and are verified live instead).
/// </summary>
internal static class IdentityTestHarness
{
    public static (UserManager<ApplicationUser> UserManager, IPasswordHasher<ApplicationUser> PasswordHasher, IdentityAccessDbContext DbContext) Create()
    {
        var dbContext = new IdentityAccessDbContext(
            new DbContextOptionsBuilder<IdentityAccessDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton(dbContext);
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<IdentityAccessDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<UserManager<ApplicationUser>>(), provider.GetRequiredService<IPasswordHasher<ApplicationUser>>(), dbContext);
    }
}
