using HAMS.Platform.Notifications.Application;
using HAMS.Platform.Notifications.Domain;
using HAMS.Platform.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Notifications.Tests;

public class NotificationAdminServiceTests
{
    private static readonly Guid SmsChannelId = new("00000000-0000-0000-0017-000000000001");
    private static readonly Guid EmailChannelId = new("00000000-0000-0000-0017-000000000002");

    private static NotificationsDbContext CreateContext()
    {
        var db = new NotificationsDbContext(
            new DbContextOptionsBuilder<NotificationsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        // HasData-seeded rows (the real migration's seed data) aren't materialized against a bare
        // InMemory database the way they are via a real migration, so the fixture channels this
        // test needs for the join in GetEntriesAsync have to be added explicitly here.
        db.NotificationChannels.AddRange(
            new NotificationChannel { Id = SmsChannelId, Code = NotificationChannelCodes.Sms, Name = "SMS", DisplayOrder = 1 },
            new NotificationChannel { Id = EmailChannelId, Code = NotificationChannelCodes.Email, Name = "Email", DisplayOrder = 2 });
        db.SaveChanges();

        return db;
    }

    private static NotificationOutboxEntry CreateEntry(Guid channelId, NotificationDeliveryStatus status, int attemptCount = 0) => new()
    {
        Id = Guid.NewGuid(),
        ChannelId = channelId,
        Recipient = "7771234",
        Body = "Test message",
        Status = status,
        AttemptCount = attemptCount,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task GetEntriesAsync_returns_every_entry_when_no_status_filter_is_given()
    {
        await using var db = CreateContext();
        db.NotificationOutboxEntries.AddRange(
            CreateEntry(SmsChannelId, NotificationDeliveryStatus.Pending),
            CreateEntry(EmailChannelId, NotificationDeliveryStatus.Sent),
            CreateEntry(SmsChannelId, NotificationDeliveryStatus.Failed));
        await db.SaveChangesAsync();
        var service = new NotificationAdminService(db);

        var entries = await service.GetEntriesAsync(status: null);

        Assert.Equal(3, entries.Count);
    }

    [Fact]
    public async Task GetEntriesAsync_filters_by_status_and_resolves_the_channel_code()
    {
        await using var db = CreateContext();
        db.NotificationOutboxEntries.AddRange(
            CreateEntry(SmsChannelId, NotificationDeliveryStatus.Failed),
            CreateEntry(EmailChannelId, NotificationDeliveryStatus.Sent));
        await db.SaveChangesAsync();
        var service = new NotificationAdminService(db);

        var entries = await service.GetEntriesAsync(NotificationDeliveryStatus.Failed);

        var entry = Assert.Single(entries);
        Assert.Equal(NotificationChannelCodes.Sms, entry.ChannelCode);
    }

    [Fact]
    public async Task RetryAsync_moves_a_Failed_entry_back_to_Pending()
    {
        await using var db = CreateContext();
        var entry = CreateEntry(SmsChannelId, NotificationDeliveryStatus.Failed, attemptCount: 5);
        db.NotificationOutboxEntries.Add(entry);
        await db.SaveChangesAsync();
        var service = new NotificationAdminService(db);

        await service.RetryAsync(entry.Id);

        var reloaded = await db.NotificationOutboxEntries.SingleAsync(e => e.Id == entry.Id);
        Assert.Equal(NotificationDeliveryStatus.Pending, reloaded.Status);
        // AttemptCount is deliberately NOT reset — the next dispatch attempt still counts against
        // the same ceiling, so a permanently-broken recipient can't be retried forever.
        Assert.Equal(5, reloaded.AttemptCount);
    }

    [Fact]
    public async Task RetryAsync_throws_when_the_entry_is_not_Failed()
    {
        await using var db = CreateContext();
        var entry = CreateEntry(SmsChannelId, NotificationDeliveryStatus.Sent);
        db.NotificationOutboxEntries.Add(entry);
        await db.SaveChangesAsync();
        var service = new NotificationAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RetryAsync(entry.Id));
    }

    [Fact]
    public async Task RetryAsync_throws_when_the_entry_does_not_exist()
    {
        await using var db = CreateContext();
        var service = new NotificationAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RetryAsync(Guid.NewGuid()));
    }
}
