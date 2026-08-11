using HAMS.Platform.Access.Domain;
using HAMS.Platform.Access.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access.Tests;

public class PersonAccessScopeQueryTests
{
    private static readonly DateOnly Today = new(2026, 8, 10);

    private static readonly Guid ClassTeacherRoleId = Guid.NewGuid();

    private static AccessDbContext CreateContext()
    {
        var db = new AccessDbContext(new DbContextOptionsBuilder<AccessDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Roles.Add(new Role { Id = ClassTeacherRoleId, Code = RoleCodes.ClassTeacher, Name = "Class Teacher" });
        db.SaveChanges();
        return db;
    }

    private static AccessGrant Grant(Guid personId, Guid? schoolId = null, Guid? classId = null, Guid? subjectId = null, DateOnly? effectiveFrom = null, DateOnly? effectiveTo = null) => new()
    {
        Id = Guid.NewGuid(),
        PersonId = personId,
        RoleId = ClassTeacherRoleId,
        SchoolId = schoolId,
        ClassId = classId,
        SubjectId = subjectId,
        EffectiveFrom = effectiveFrom ?? Today.AddDays(-30),
        EffectiveTo = effectiveTo,
        SourceType = AccessGrantSourceTypes.PersonRoleAssignment,
        SourceId = Guid.NewGuid(),
    };

    [Fact]
    public async Task Returns_only_the_given_persons_active_grants_as_summaries()
    {
        var personId = Guid.NewGuid();
        var otherPersonId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        await using var db = CreateContext();
        db.AccessGrants.Add(Grant(personId, schoolId: schoolId, classId: classId));
        db.AccessGrants.Add(Grant(otherPersonId, schoolId: Guid.NewGuid())); // a different person - must not leak in
        await db.SaveChangesAsync();

        var query = new PersonAccessScopeQuery(db);
        var grants = await query.GetActiveGrantsAsync(personId, Today);

        var grant = Assert.Single(grants);
        Assert.Equal(schoolId, grant.SchoolId);
        Assert.Equal(classId, grant.ClassId);
        Assert.Equal(RoleCodes.ClassTeacher, grant.RoleCode);
    }

    [Fact]
    public async Task Excludes_a_grant_that_has_expired_or_not_started_yet()
    {
        var personId = Guid.NewGuid();
        await using var db = CreateContext();
        db.AccessGrants.Add(Grant(personId, schoolId: Guid.NewGuid(), effectiveFrom: Today.AddDays(-60), effectiveTo: Today.AddDays(-1))); // expired
        db.AccessGrants.Add(Grant(personId, schoolId: Guid.NewGuid(), effectiveFrom: Today.AddDays(1))); // not started
        await db.SaveChangesAsync();

        var query = new PersonAccessScopeQuery(db);
        var grants = await query.GetActiveGrantsAsync(personId, Today);

        Assert.Empty(grants);
    }
}
