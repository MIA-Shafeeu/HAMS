using HAMS.Platform.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Notifications.Infrastructure;

/// <summary>Owns the "notifications" schema exclusively (build plan §2: one schema per module) — written to by every module via <see cref="Application.INotificationOutboxWriter"/>.</summary>
public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<NotificationChannel> NotificationChannels => Set<NotificationChannel>();
    public DbSet<NotificationOutboxEntry> NotificationOutboxEntries => Set<NotificationOutboxEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.Entity<NotificationChannel>(entity =>
        {
            entity.ToTable("NotificationChannels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(NotificationsSeedData.NotificationChannels);
        });

        modelBuilder.Entity<NotificationOutboxEntry>(entity =>
        {
            entity.ToTable("NotificationOutboxEntries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Recipient).HasMaxLength(320).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.Body).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.LastError).HasMaxLength(1000);
            entity.HasIndex(e => e.Status);
            entity.HasOne<NotificationChannel>().WithMany().HasForeignKey(e => e.ChannelId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
