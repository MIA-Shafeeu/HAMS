using HAMS.Platform.Access.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access.Infrastructure;

/// <summary>Owns the "access" schema exclusively (build plan §2: one schema per module/kernel).</summary>
public sealed class AccessDbContext(DbContextOptions<AccessDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<PersonRoleAssignment> PersonRoleAssignments => Set<PersonRoleAssignment>();
    public DbSet<AccessGrant> AccessGrants => Set<AccessGrant>();
    public DbSet<ConfidentialityTier> ConfidentialityTiers => Set<ConfidentialityTier>();
    public DbSet<ConfidentialAccessGrant> ConfidentialAccessGrants => Set<ConfidentialAccessGrant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("access");

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasData(AccessSeedData.Roles);
        });

        modelBuilder.Entity<PersonRoleAssignment>(entity =>
        {
            entity.ToTable("PersonRoleAssignments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PersonId, e.EffectiveFrom, e.EffectiveTo });
            entity.HasOne<Role>().WithMany().HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccessGrant>(entity =>
        {
            entity.ToTable("AccessGrants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceType).HasMaxLength(100).IsRequired();
            // The hottest query path in the system (build plan §4) — every scope check filters on
            // PersonId + effective-dating first, then narrows by whichever scope columns the
            // target resource populates.
            entity.HasIndex(e => new { e.PersonId, e.EffectiveFrom, e.EffectiveTo });
            entity.HasIndex(e => new { e.SourceType, e.SourceId });
            entity.HasOne<Role>().WithMany().HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ConfidentialityTier>().WithMany()
                .HasForeignKey(e => e.ConfidentialityTierId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConfidentialityTier>(entity =>
        {
            entity.ToTable("ConfidentialityTiers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasData(AccessSeedData.ConfidentialityTiers);
        });

        modelBuilder.Entity<ConfidentialAccessGrant>(entity =>
        {
            entity.ToTable("ConfidentialAccessGrants");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PersonId, e.StudentId, e.EffectiveFrom, e.EffectiveTo });
            entity.HasOne<ConfidentialityTier>().WithMany()
                .HasForeignKey(e => e.ConfidentialityTierId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
