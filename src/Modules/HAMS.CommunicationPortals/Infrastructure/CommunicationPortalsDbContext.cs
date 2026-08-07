using HAMS.CommunicationPortals.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.CommunicationPortals.Infrastructure;

/// <summary>
/// Owns the "portals" schema exclusively (build plan §2: one schema per module) — this module's
/// first-ever <c>DbContext</c>; every prior phase was pure read-orchestration over sibling modules'
/// own schemas with nothing of its own to persist. <see cref="GuardianAcknowledgement"/> (Phase 13)
/// is the first thing this module actually owns.
/// </summary>
public sealed class CommunicationPortalsDbContext(DbContextOptions<CommunicationPortalsDbContext> options) : DbContext(options)
{
    public DbSet<GuardianAcknowledgement> GuardianAcknowledgements => Set<GuardianAcknowledgement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("portals");

        modelBuilder.Entity<GuardianAcknowledgement>(entity =>
        {
            entity.ToTable("GuardianAcknowledgements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).HasMaxLength(200).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => new { e.GuardianPersonId, e.StudentPersonId, e.EntityType, e.EntityId }).IsUnique();
        });
    }
}
