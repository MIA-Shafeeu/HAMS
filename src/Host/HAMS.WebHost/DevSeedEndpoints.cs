using HAMS.IdentityAccess.Application.Auth;
using HAMS.IdentityAccess.Domain;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Domain;
using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Common.Contracts;
using HAMS.TeachingTimetable.Application;
using Microsoft.AspNetCore.Identity;

namespace HAMS.WebHost;

public sealed record SeededStaffCredential(string RoleCode, string RoleName, string Username, string Password, bool AlreadyExisted);

/// <summary>
/// Dev-only test-data helper: one login per staff <see cref="RoleCodes"/> value, each given a real,
/// correctly-scoped assignment (not just a bare role) wherever the dev database already has a
/// School/Class/Subject to scope it to — so these accounts actually exercise the Class/Subject/
/// Leading-Teacher/whole-school access-scoping rules (<c>IStaffAccessScopeQuery</c>), not just page-
/// level role gates. NEVER mapped outside Development - see the
/// <c>app.Environment.IsDevelopment()</c> guard around <c>MapDevSeedEndpoints()</c> in
/// <c>Program.cs</c>, the same gate <c>DevelopmentDataSeeder</c> uses. An unauthenticated endpoint
/// that hands out real account passwords would be a genuine account-takeover surface in Production.
///
/// Lives directly in HAMS.WebHost, unlike every other endpoint file (which lives inside its owning
/// module) - no single module has every dependency this needs (IdentityAccess's account/role
/// services, PeopleEnrollment's person/staff-profile service, OrgCurriculum's structure lookup,
/// TeachingTimetable's three assignment services all at once), and adding those cross-module
/// references to a real module just for a throwaway dev convenience isn't worth the coupling -
/// HAMS.WebHost already depends on all of them as the composition root.
/// </summary>
internal static class DevSeedEndpoints
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
            IOrgStructureLookup orgLookup,
            IClassTeacherAssignmentService classTeacherAssignments,
            ISubjectTeachingAssignmentService subjectTeachingAssignments,
            ILeadingTeacherAssignmentService leadingTeacherAssignments,
            IClock clock,
            CancellationToken cancellationToken) =>
        {
            // Whatever this dev database's first School/Year/Grade/Class/Subject happen to be -
            // good enough to give Class/Subject/Leading Teacher and Principal-type roles a REAL,
            // meaningful scope to test against, without this seeder needing to invent its own
            // parallel org structure. Any that don't exist yet are simply left null below, and the
            // corresponding role's scoped assignment is skipped rather than failing outright - a
            // brand new database with no School yet still seeds working LOGINS, just unscoped ones.
            var school = (await orgLookup.GetSchoolsAsync(cancellationToken)).FirstOrDefault();
            var year = school is null ? null : (await orgLookup.GetAcademicYearsAsync(school.Id, cancellationToken)).FirstOrDefault();
            var subject = school is null ? null : (await orgLookup.GetSubjectsAsync(school.Id, cancellationToken)).FirstOrDefault();
            var cls = year is null ? null : (await orgLookup.GetClassesAsync(year.Id, cancellationToken)).FirstOrDefault();

            var results = new List<SeededStaffCredential>();

            foreach (var (roleCode, roleName) in StaffRoles)
            {
                var username = roleCode.ToLowerInvariant();
                var existingUser = await userManager.FindByNameAsync(username);
                if (existingUser is not null)
                {
                    // Only the login itself is checked for idempotency - re-running this against an
                    // already-seeded database won't retroactively add/fix a scoped assignment for
                    // an account that already exists. Fine for a dev tool; not meant to converge
                    // like a real migration would.
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

                // System Administrator is the one role genuinely meant to be unrestricted - every
                // other role gets the most specific real scope this database can offer, matching
                // exactly how a real admin would grant it (School Administrator/Principal/Deputy
                // Principal via the generic Assign Role screen with a real School; Class/Subject/
                // Leading Teacher via the Teaching Assignments screen's own dedicated tabs, never
                // the generic one) - see StaffAccessScopeQuery's own remarks for why a bare role
                // assignment with no Class/Subject can't just mean "whole school" for those three.
                switch (roleCode)
                {
                    case RoleCodes.SystemAdministrator:
                        await roleAssignments.AssignRoleAsync(personId, roleCode, schoolId: null, clock.TodayUtc, null, cancellationToken);
                        break;

                    case RoleCodes.SchoolAdministrator or RoleCodes.Principal or RoleCodes.DeputyPrincipal when school is not null:
                        await roleAssignments.AssignRoleAsync(personId, roleCode, school.Id, clock.TodayUtc, null, cancellationToken);
                        break;

                    case RoleCodes.ClassTeacher when cls is not null && year is not null:
                        await classTeacherAssignments.AssignAsync(personId, cls.Id, year.Id, school!.Id, clock.TodayUtc, null, cancellationToken);
                        break;

                    case RoleCodes.SubjectTeacher when cls is not null && year is not null && subject is not null:
                        await subjectTeachingAssignments.AssignAsync(personId, subject.Id, cls.Id, year.Id, school!.Id, clock.TodayUtc, null, cancellationToken);
                        break;

                    case RoleCodes.LeadingTeacher when year is not null && subject is not null:
                        await leadingTeacherAssignments.AssignAsync(personId, subject.Id, year.Id, school!.Id, clock.TodayUtc, null, cancellationToken);
                        break;
                }

                results.Add(new SeededStaffCredential(roleCode, roleName, username, DevPassword, AlreadyExisted: false));
            }

            return Results.Ok(results);
        }).AllowAnonymous();

        return endpoints;
    }
}
