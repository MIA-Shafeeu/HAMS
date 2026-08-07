using HAMS.IdentityAccess.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HAMS.IdentityAccess.Infrastructure;

/// <summary>
/// Owns the "identity" schema exclusively (build plan §2: one schema per module). Extends
/// <see cref="IdentityUserContext{TUser,TKey}"/> rather than the full <c>IdentityDbContext</c> —
/// this module never uses ASP.NET Core Identity's own role/claims-based role system (see
/// <see cref="ApplicationUser"/>'s remarks).
/// </summary>
public sealed class IdentityAccessDbContext(DbContextOptions<IdentityAccessDbContext> options)
    : IdentityUserContext<ApplicationUser, Guid>(options)
{
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<GuardianOtpChallenge> GuardianOtpChallenges => Set<GuardianOtpChallenge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.PersonId).IsRequired();
            entity.HasIndex(e => e.PersonId);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RefreshTokenHash).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.RefreshTokenHash).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GuardianOtpChallenge>(entity =>
        {
            entity.ToTable("GuardianOtpChallenges");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CodeHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.PhoneNumber);
        });
    }
}
