using HAMS.Intervention.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Intervention.Infrastructure;

/// <summary>Owns the "intervention" schema exclusively (build plan §2: one schema per module).</summary>
public sealed class InterventionDbContext(DbContextOptions<InterventionDbContext> options) : DbContext(options)
{
    public DbSet<InterventionType> InterventionTypes => Set<InterventionType>();
    public DbSet<InterventionCase> InterventionCases => Set<InterventionCase>();
    public DbSet<InterventionPlan> InterventionPlans => Set<InterventionPlan>();
    public DbSet<ReassessmentAttempt> ReassessmentAttempts => Set<ReassessmentAttempt>();
    public DbSet<TopicClosure> TopicClosures => Set<TopicClosure>();
    public DbSet<CarriedForwardGap> CarriedForwardGaps => Set<CarriedForwardGap>();
    public DbSet<BehaviourCategory> BehaviourCategories => Set<BehaviourCategory>();
    public DbSet<BehaviourIncident> BehaviourIncidents => Set<BehaviourIncident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("intervention");

        modelBuilder.Entity<InterventionType>(entity =>
        {
            entity.ToTable("InterventionTypes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(InterventionSeedData.InterventionTypes);
        });

        modelBuilder.Entity<InterventionCase>(entity =>
        {
            entity.ToTable("InterventionCases");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfidentialityTierCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => new { e.StudentPersonId, e.SubjectId, e.Status });
            entity.HasOne<InterventionType>().WithMany().HasForeignKey(e => e.InterventionTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InterventionPlan>(entity =>
        {
            entity.ToTable("InterventionPlans");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => e.InterventionCaseId);
            entity.HasOne<InterventionCase>().WithMany().HasForeignKey(e => e.InterventionCaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReassessmentAttempt>(entity =>
        {
            entity.ToTable("ReassessmentAttempts");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InterventionCaseId);
            entity.HasOne<InterventionCase>().WithMany().HasForeignKey(e => e.InterventionCaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TopicClosure>(entity =>
        {
            entity.ToTable("TopicClosures");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReviewNotes).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => new { e.TeachingTopicId, e.CreatedAtUtc });
        });

        modelBuilder.Entity<CarriedForwardGap>(entity =>
        {
            entity.ToTable("CarriedForwardGaps");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentPersonId, e.LearningOutcomeId });
            entity.HasOne<TopicClosure>().WithMany().HasForeignKey(e => e.TopicClosureId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<InterventionCase>().WithMany().HasForeignKey(e => e.InterventionCaseId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BehaviourCategory>(entity =>
        {
            entity.ToTable("BehaviourCategories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(InterventionSeedData.BehaviourCategories);
        });

        modelBuilder.Entity<BehaviourIncident>(entity =>
        {
            entity.ToTable("BehaviourIncidents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.ActionTaken).HasMaxLength(2000);
            entity.Property(e => e.ReviewNotes).HasMaxLength(2000);
            entity.Property(e => e.ConfidentialityTierCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => new { e.StudentPersonId, e.OccurredDate });
            entity.HasOne<BehaviourCategory>().WithMany().HasForeignKey(e => e.BehaviourCategoryId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
