using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Domain;
using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using HAMS.IdentityAccess.Application.Auth;
using HAMS.IdentityAccess.Domain;

namespace HAMS.IdentityAccess.Endpoints;

public sealed record SeededStaffCredential(string RoleCode, string RoleName, string Username, string Password, bool AlreadyExisted);

/// <summary>
/// Dev-only test-data helper: one login per staff <see cref="RoleCodes"/> value, for manually
/// clicking through role-gated pages during local testing. NEVER mapped outside Development - see
/// the <c>app.Environment.IsDevelopment()</c> guard around <c>MapDevSeedEndpoints()</c> in
/// <c>Program.cs</c>, the same gate <c>DevelopmentDataSeeder</c> uses. An unauthenticated endpoint
/// that hands out real account passwords would be a genuine account-takeover surface in Production,
/// so unlike <see cref="SetupEndpoints"/> (deliberately reachable in prod for the one-time real
/// bootstrap), this must never exist there at all.
/// </summary>
// Public, unlike every other file in this Endpoints/ folder: those are only ever called from
// IdentityAccessModule.MapIdentityAccessModuleEndpoints() in the same assembly. This one must be
// callable directly from HAMS.WebHost's Program.cs (a different assembly) so it can be mapped
// conditionally on app.Environment.IsDevelopment() there, instead of unconditionally like the rest.
public static class DevSeedEndpoints
{
    // Fixed, well-known, non-secret dev password - shared with the seeded bootstrap admin
    // (Bootstrap:AdminPassword in appsettings.Development.json) so every dev/test login uses one
    // password to remember. Never used outside a Development-gated endpoint.
    private const string DevPassword = "Dev-Only-Password-1!";

    // The one Island PeopleSeedData.cs guarantees exists in every migrated database (Hirilandhoo,
    // Thaa Atoll) - referenced by id since PeopleSeedData is internal to HAMS.PeopleEnrollment.
    private static readonly Guid HirilandhooIslandId = new("00000000-0000-0000-0009-000000000001");

    // Only the roles that represent an actual school employee get a StaffProfile - Regulatory
    // Officer/School Inspector/Auditor are external oversight roles in the SRS, not staff, and
    // Student/Guardian have their own dedicated seeding concerns (a different kind of test data
    // entirely), so neither is covered by this "staff" seeder.
    private static readonly (string Code, string Name)[] StaffRoles =
    [
        (RoleCodes.SystemAdministrator, "System Administrator"),
        (RoleCodes.SchoolAdministrator, "School Administrator"),
        (RoleCodes.Principal, "Principal"),
        (RoleCodes.DeputyPrincipal, "Deputy Principal"),
        (RoleCodes.ClassTeacher, "Class Teacher"),
        (RoleCodes.SubjectTeacher, "Subject Teacher"),
        (RoleCodes.LeadingTeacher, "Leading Teacher"),
    ];

    public static IEndpointRouteBuilder MapDevSeedEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/seeder/staff", async (
            UserManager<ApplicationUser> userManager,
            IPeopleAdminService peopleAdmin,
            IStaffAccountService staffAccounts,
            IPersonRoleAssignmentService roleAssignments,
            IClock clock,
            CancellationToken cancellationToken) =>
        {
            var results = new List<SeededStaffCredential>();

            foreach (var (roleCode, roleName) in StaffRoles)
            {
                var username = roleCode.ToLowerInvariant();
                var existingUser = await userManager.FindByNameAsync(username);
                if (existingUser is not null)
                {
                    results.Add(new SeededStaffCredential(roleCode, roleName, username, DevPassword, AlreadyExisted: true));
                    continue;
                }

                var personId = await peopleAdmin.CreatePersonAsync(
                    nameEn: $"Dev {roleName}",
                    nameDv: $"Dev {roleName}",
                    dateOfBirth: new DateOnly(1990, 1, 1),
                    address: new Address
                    {
                        IslandId = HirilandhooIslandId,
                        RoadEn = "Test Road", RoadDv = "Test Road",
                        HouseNameEn = "Test House", HouseNameDv = "Test House",
                    },
                    phoneNumber: null,
                    email: $"{username}@dev.hams.local",
                    cancellationToken);

                await peopleAdmin.CreateStaffProfileAsync(
                    personId, employeeNumber: $"DEV-{roleCode}", hireDate: clock.TodayUtc,
                    employmentStatusCode: EmploymentStatusCodes.Active, cancellationToken);

                await staffAccounts.CreateAccountAsync(personId, username, $"{username}@dev.hams.local", DevPassword, cancellationToken);

                await roleAssignments.AssignRoleAsync(
                    personId, roleCode, schoolId: null, effectiveFrom: clock.TodayUtc, effectiveTo: null, cancellationToken);

                results.Add(new SeededStaffCredential(roleCode, roleName, username, DevPassword, AlreadyExisted: false));
            }

            return Results.Ok(results);
        }).AllowAnonymous();

        return endpoints;
    }
}
