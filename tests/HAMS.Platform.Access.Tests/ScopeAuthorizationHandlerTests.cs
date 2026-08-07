using System.Security.Claims;
using HAMS.Platform.Access.Authorization;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Access.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access.Tests;

public class ScopeAuthorizationHandlerTests
{
    private static readonly DateOnly Today = new(2026, 8, 4);

    private static AccessDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AccessDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Succeeds_when_grant_wildcards_every_dimension_the_resource_populates()
    {
        var personId = Guid.NewGuid();
        await using var db = CreateContext();
        db.AccessGrants.Add(new AccessGrant
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            RoleId = Guid.NewGuid(),
            EffectiveFrom = Today.AddDays(-30),
            SourceType = AccessGrantSourceTypes.PersonRoleAssignment,
            SourceId = Guid.NewGuid(),
        });
        await db.SaveChangesAsync();

        var resource = new FakeScopedResource(SchoolId: Guid.NewGuid(), GradeId: Guid.NewGuid());

        Assert.True(await EvaluateAsync(db, personId, resource));
    }

    [Fact]
    public async Task Fails_when_grant_scopes_to_a_different_grade_than_the_resource()
    {
        var personId = Guid.NewGuid();
        await using var db = CreateContext();
        db.AccessGrants.Add(new AccessGrant
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            RoleId = Guid.NewGuid(),
            GradeId = Guid.NewGuid(), // a specific grade, not the one the resource belongs to
            EffectiveFrom = Today.AddDays(-30),
            SourceType = AccessGrantSourceTypes.PersonRoleAssignment,
            SourceId = Guid.NewGuid(),
        });
        await db.SaveChangesAsync();

        var resource = new FakeScopedResource(GradeId: Guid.NewGuid());

        Assert.False(await EvaluateAsync(db, personId, resource));
    }

    [Fact]
    public async Task Succeeds_when_a_grade_scoped_grant_matches_the_resources_grade()
    {
        var personId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        await using var db = CreateContext();
        db.AccessGrants.Add(new AccessGrant
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            RoleId = Guid.NewGuid(),
            GradeId = gradeId,
            EffectiveFrom = Today.AddDays(-30),
            SourceType = AccessGrantSourceTypes.PersonRoleAssignment,
            SourceId = Guid.NewGuid(),
        });
        await db.SaveChangesAsync();

        var resource = new FakeScopedResource(GradeId: gradeId);

        Assert.True(await EvaluateAsync(db, personId, resource));
    }

    [Fact]
    public async Task Fails_when_the_matching_grant_has_already_expired()
    {
        var personId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        await using var db = CreateContext();
        db.AccessGrants.Add(new AccessGrant
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            RoleId = Guid.NewGuid(),
            GradeId = gradeId,
            EffectiveFrom = Today.AddYears(-1),
            EffectiveTo = Today.AddDays(-1),
            SourceType = AccessGrantSourceTypes.PersonRoleAssignment,
            SourceId = Guid.NewGuid(),
        });
        await db.SaveChangesAsync();

        var resource = new FakeScopedResource(GradeId: gradeId);

        Assert.False(await EvaluateAsync(db, personId, resource));
    }

    [Fact]
    public async Task Fails_when_caller_is_not_authenticated()
    {
        await using var db = CreateContext();
        var resource = new FakeScopedResource();

        Assert.False(await EvaluateAsync(db, personId: null, resource));
    }

    private static async Task<bool> EvaluateAsync(AccessDbContext db, Guid? personId, IScopedResource resource)
    {
        var handler = new ScopeAuthorizationHandler(db, new FakeCurrentUser { PersonId = personId }, new FakeClock(Today));
        var context = new AuthorizationHandlerContext([ScopeRequirement.Instance], new ClaimsPrincipal(), resource);

        await handler.HandleAsync(context);

        return context.HasSucceeded;
    }
}
