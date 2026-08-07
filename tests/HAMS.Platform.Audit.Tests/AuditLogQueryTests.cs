using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Audit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Audit.Tests;

public class AuditLogQueryTests
{
    private static AuditDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AuditDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AuditLogEntry CreateEntry(
        DateTimeOffset occurredAtUtc, AuditAction action = AuditAction.Create, string entityType = "Widget",
        Guid? actorPersonId = null, string summary = "Something happened.") => new()
    {
        OccurredAtUtc = occurredAtUtc, Action = action, EntityType = entityType, ActorPersonId = actorPersonId, Summary = summary,
    };

    [Fact]
    public async Task SearchAsync_returns_all_entries_ordered_most_recent_first_when_unfiltered()
    {
        await using var db = CreateContext();
        var older = CreateEntry(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var newer = CreateEntry(new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero));
        db.AuditLogEntries.AddRange(older, newer);
        await db.SaveChangesAsync();
        var query = new AuditLogQuery(db);

        var result = await query.SearchAsync(new AuditLogSearchRequest());

        Assert.Equal(2, result.TotalCount);
        Assert.Equal([newer.Id, older.Id], result.Entries.Select(e => e.Id));
    }

    [Fact]
    public async Task SearchAsync_filters_by_date_range()
    {
        await using var db = CreateContext();
        var inRange = CreateEntry(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var beforeRange = CreateEntry(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var afterRange = CreateEntry(new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero));
        db.AuditLogEntries.AddRange(inRange, beforeRange, afterRange);
        await db.SaveChangesAsync();
        var query = new AuditLogQuery(db);

        var result = await query.SearchAsync(new AuditLogSearchRequest(
            FromUtc: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            ToUtc: new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero)));

        Assert.Single(result.Entries, e => e.Id == inRange.Id);
    }

    [Fact]
    public async Task SearchAsync_filters_by_action()
    {
        await using var db = CreateContext();
        var login = CreateEntry(DateTimeOffset.UtcNow, action: AuditAction.Login);
        var create = CreateEntry(DateTimeOffset.UtcNow, action: AuditAction.Create);
        db.AuditLogEntries.AddRange(login, create);
        await db.SaveChangesAsync();
        var query = new AuditLogQuery(db);

        var result = await query.SearchAsync(new AuditLogSearchRequest(Action: AuditAction.Login));

        Assert.Single(result.Entries, e => e.Id == login.Id);
    }

    [Fact]
    public async Task SearchAsync_filters_by_entity_type()
    {
        await using var db = CreateContext();
        var widget = CreateEntry(DateTimeOffset.UtcNow, entityType: "Widget");
        var gadget = CreateEntry(DateTimeOffset.UtcNow, entityType: "Gadget");
        db.AuditLogEntries.AddRange(widget, gadget);
        await db.SaveChangesAsync();
        var query = new AuditLogQuery(db);

        var result = await query.SearchAsync(new AuditLogSearchRequest(EntityType: "Gadget"));

        Assert.Single(result.Entries, e => e.Id == gadget.Id);
    }

    [Fact]
    public async Task SearchAsync_filters_by_actor_person_id()
    {
        await using var db = CreateContext();
        var actorId = Guid.NewGuid();
        var byActor = CreateEntry(DateTimeOffset.UtcNow, actorPersonId: actorId);
        var bySomeoneElse = CreateEntry(DateTimeOffset.UtcNow, actorPersonId: Guid.NewGuid());
        db.AuditLogEntries.AddRange(byActor, bySomeoneElse);
        await db.SaveChangesAsync();
        var query = new AuditLogQuery(db);

        var result = await query.SearchAsync(new AuditLogSearchRequest(ActorPersonId: actorId));

        Assert.Single(result.Entries, e => e.Id == byActor.Id);
    }

    [Fact]
    public async Task SearchAsync_filters_by_free_text_search_against_summary()
    {
        await using var db = CreateContext();
        var matching = CreateEntry(DateTimeOffset.UtcNow, summary: "Staff sign-in: admin.");
        var nonMatching = CreateEntry(DateTimeOffset.UtcNow, summary: "Result published.");
        db.AuditLogEntries.AddRange(matching, nonMatching);
        await db.SaveChangesAsync();
        var query = new AuditLogQuery(db);

        var result = await query.SearchAsync(new AuditLogSearchRequest(SearchText: "sign-in"));

        Assert.Single(result.Entries, e => e.Id == matching.Id);
    }

    [Fact]
    public async Task SearchAsync_paginates_correctly()
    {
        await using var db = CreateContext();
        for (var i = 0; i < 5; i++)
        {
            db.AuditLogEntries.Add(CreateEntry(new DateTimeOffset(2026, 1, 1 + i, 0, 0, 0, TimeSpan.Zero)));
        }

        await db.SaveChangesAsync();
        var query = new AuditLogQuery(db);

        var page1 = await query.SearchAsync(new AuditLogSearchRequest(Page: 1, PageSize: 2));
        var page2 = await query.SearchAsync(new AuditLogSearchRequest(Page: 2, PageSize: 2));

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Entries.Count);
        Assert.Equal(2, page2.Entries.Count);
        Assert.Empty(page1.Entries.Select(e => e.Id).Intersect(page2.Entries.Select(e => e.Id)));
    }

    [Fact]
    public async Task GetDistinctEntityTypesAsync_returns_distinct_values_sorted()
    {
        await using var db = CreateContext();
        db.AuditLogEntries.AddRange(
            CreateEntry(DateTimeOffset.UtcNow, entityType: "Widget"),
            CreateEntry(DateTimeOffset.UtcNow, entityType: "Widget"),
            CreateEntry(DateTimeOffset.UtcNow, entityType: "Gadget"));
        await db.SaveChangesAsync();
        var query = new AuditLogQuery(db);

        var result = await query.GetDistinctEntityTypesAsync();

        Assert.Equal(["Gadget", "Widget"], result);
    }
}
