using HAMS.Platform.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Audit.Infrastructure;

/// <summary>
/// Owns the "audit" schema exclusively — the one, insert-only audit trail every module writes to
/// via <see cref="IAuditLogWriter"/> (build plan §1.4/§2: one schema per module/kernel).
/// </summary>
public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("audit");

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("AuditLogEntries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).HasMaxLength(200).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.Summary).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.ActorPersonId);
            entity.HasIndex(e => e.OccurredAtUtc);
        });
    }
}
