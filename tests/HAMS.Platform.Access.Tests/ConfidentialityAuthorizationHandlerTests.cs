using System.Security.Claims;
using HAMS.Platform.Access.Authorization;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Access.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access.Tests;

public class ConfidentialityAuthorizationHandlerTests
{
    private static readonly DateOnly Today = new(2026, 8, 4);

    private static AccessDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AccessDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<ConfidentialityTier> SeedTierAsync(AccessDbContext db, string code, int rank)
    {
        var tier = new ConfidentialityTier { Id = Guid.NewGuid(), Code = code, Name = code, Rank = rank };
        db.ConfidentialityTiers.Add(tier);
        await db.SaveChangesAsync();
        return tier;
    }

    [Fact]
    public async Task Succeeds_trivially_when_the_resource_is_not_confidential()
    {
        await using var db = CreateContext();
        var resource = new FakeScopedResource(ConfidentialityTierCode: null);

        Assert.True(await EvaluateAsync(db, personId: null, resource));
    }

    [Fact]
    public async Task Fails_when_caller_has_no_confidential_access_grant_at_all()
    {
        await using var db = CreateContext();
        var tier = await SeedTierAsync(db, ConfidentialityTierCodes.Restricted, rank: 1);

        var resource = new FakeScopedResource(StudentId: Guid.NewGuid(), ConfidentialityTierCode: tier.Code);

        Assert.False(await EvaluateAsync(db, Guid.NewGuid(), resource));
    }

    [Fact]
    public async Task Succeeds_when_caller_has_a_matching_tier_grant_for_the_specific_student()
    {
        var personId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        await using var db = CreateContext();
        var tier = await SeedTierAsync(db, ConfidentialityTierCodes.Restricted, rank: 1);
        db.ConfidentialAccessGrants.Add(new ConfidentialAccessGrant
        {
            Id = Guid.NewGuid(), PersonId = personId, StudentId = studentId, ConfidentialityTierId = tier.Id,
            EffectiveFrom = Today.AddDays(-10),
        });
        await db.SaveChangesAsync();

        var resource = new FakeScopedResource(StudentId: studentId, ConfidentialityTierCode: tier.Code);

        Assert.True(await EvaluateAsync(db, personId, resource));
    }

    [Fact]
    public async Task Fails_when_callers_grant_is_for_a_different_student()
    {
        var personId = Guid.NewGuid();
        await using var db = CreateContext();
        var tier = await SeedTierAsync(db, ConfidentialityTierCodes.Restricted, rank: 1);
        db.ConfidentialAccessGrants.Add(new ConfidentialAccessGrant
        {
            Id = Guid.NewGuid(), PersonId = personId, StudentId = Guid.NewGuid(), ConfidentialityTierId = tier.Id,
            EffectiveFrom = Today.AddDays(-10),
        });
        await db.SaveChangesAsync();

        var resource = new FakeScopedResource(StudentId: Guid.NewGuid(), ConfidentialityTierCode: tier.Code);

        Assert.False(await EvaluateAsync(db, personId, resource));
    }

    [Fact]
    public async Task Succeeds_when_a_wildcard_student_grant_covers_any_student_at_that_tier()
    {
        var personId = Guid.NewGuid(); // e.g. a Safeguarding Lead with a blanket grant
        await using var db = CreateContext();
        var tier = await SeedTierAsync(db, ConfidentialityTierCodes.Safeguarding, rank: 2);
        db.ConfidentialAccessGrants.Add(new ConfidentialAccessGrant
        {
            Id = Guid.NewGuid(), PersonId = personId, StudentId = null, ConfidentialityTierId = tier.Id,
            EffectiveFrom = Today.AddDays(-10),
        });
        await db.SaveChangesAsync();

        var resource = new FakeScopedResource(StudentId: Guid.NewGuid(), ConfidentialityTierCode: tier.Code);

        Assert.True(await EvaluateAsync(db, personId, resource));
    }

    [Fact]
    public async Task Succeeds_when_callers_grant_tier_ranks_higher_than_the_resources_required_tier()
    {
        var personId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        await using var db = CreateContext();
        var restricted = await SeedTierAsync(db, ConfidentialityTierCodes.Restricted, rank: 1);
        var safeguarding = await SeedTierAsync(db, ConfidentialityTierCodes.Safeguarding, rank: 2);
        db.ConfidentialAccessGrants.Add(new ConfidentialAccessGrant
        {
            Id = Guid.NewGuid(), PersonId = personId, StudentId = studentId, ConfidentialityTierId = safeguarding.Id,
            EffectiveFrom = Today.AddDays(-10),
        });
        await db.SaveChangesAsync();

        // Resource only requires the lower Restricted tier — the higher Safeguarding grant must cover it.
        var resource = new FakeScopedResource(StudentId: studentId, ConfidentialityTierCode: restricted.Code);

        Assert.True(await EvaluateAsync(db, personId, resource));
    }

    [Fact]
    public async Task Fails_when_callers_grant_tier_ranks_lower_than_the_resources_required_tier()
    {
        var personId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        await using var db = CreateContext();
        var restricted = await SeedTierAsync(db, ConfidentialityTierCodes.Restricted, rank: 1);
        var safeguarding = await SeedTierAsync(db, ConfidentialityTierCodes.Safeguarding, rank: 2);
        db.ConfidentialAccessGrants.Add(new ConfidentialAccessGrant
        {
            Id = Guid.NewGuid(), PersonId = personId, StudentId = studentId, ConfidentialityTierId = restricted.Id,
            EffectiveFrom = Today.AddDays(-10),
        });
        await db.SaveChangesAsync();

        // Resource requires the higher Safeguarding tier — a mere Restricted grant must not cover it.
        var resource = new FakeScopedResource(StudentId: studentId, ConfidentialityTierCode: safeguarding.Code);

        Assert.False(await EvaluateAsync(db, personId, resource));
    }

    private static async Task<bool> EvaluateAsync(AccessDbContext db, Guid? personId, IScopedResource resource)
    {
        var handler = new ConfidentialityAuthorizationHandler(db, new FakeCurrentUser { PersonId = personId }, new FakeClock(Today));
        var context = new AuthorizationHandlerContext([ConfidentialityRequirement.Instance], new ClaimsPrincipal(), resource);

        await handler.HandleAsync(context);

        return context.HasSucceeded;
    }
}
