using HAMS.LearningDelivery.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Infrastructure;

/// <summary>Owns the "learning" schema exclusively (build plan §2: one schema per module).</summary>
public sealed class LearningDeliveryDbContext(DbContextOptions<LearningDeliveryDbContext> options) : DbContext(options)
{
    public DbSet<SchemeOfWork> SchemeOfWorks => Set<SchemeOfWork>();
    public DbSet<SchemeOfWorkItem> SchemeOfWorkItems => Set<SchemeOfWorkItem>();
    public DbSet<TeachingTopic> TeachingTopics => Set<TeachingTopic>();
    public DbSet<LessonPlan> LessonPlans => Set<LessonPlan>();
    public DbSet<LessonSession> LessonSessions => Set<LessonSession>();
    public DbSet<LessonSessionOutcomeCoverage> LessonSessionOutcomeCoverages => Set<LessonSessionOutcomeCoverage>();
    public DbSet<ResourceType> ResourceTypes => Set<ResourceType>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<EvidenceType> EvidenceTypes => Set<EvidenceType>();
    public DbSet<AchievementScale> AchievementScales => Set<AchievementScale>();
    public DbSet<AchievementLevel> AchievementLevels => Set<AchievementLevel>();
    public DbSet<LearningEvidence> LearningEvidences => Set<LearningEvidence>();
    public DbSet<MasteryEvaluation> MasteryEvaluations => Set<MasteryEvaluation>();
    public DbSet<KeyCompetency> KeyCompetencies => Set<KeyCompetency>();
    public DbSet<KeyCompetencyIndicator> KeyCompetencyIndicators => Set<KeyCompetencyIndicator>();
    public DbSet<KeyCompetencyEvidence> KeyCompetencyEvidences => Set<KeyCompetencyEvidence>();
    public DbSet<Homework> Homeworks => Set<Homework>();
    public DbSet<HomeworkSubmission> HomeworkSubmissions => Set<HomeworkSubmission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("learning");

        modelBuilder.Entity<SchemeOfWork>(entity =>
        {
            entity.ToTable("SchemeOfWorks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.SubjectId, e.GradeId, e.AcademicYearId }).IsUnique();
        });

        modelBuilder.Entity<SchemeOfWorkItem>(entity =>
        {
            entity.ToTable("SchemeOfWorkItems");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SchemeOfWorkId);
            entity.HasIndex(e => e.LearningOutcomeId);
            entity.HasOne<SchemeOfWork>().WithMany().HasForeignKey(e => e.SchemeOfWorkId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeachingTopic>(entity =>
        {
            entity.ToTable("TeachingTopics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NameEn).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameDv).HasMaxLength(200).IsRequired();
            entity.HasOne<SchemeOfWorkItem>().WithMany().HasForeignKey(e => e.SchemeOfWorkItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonPlan>(entity =>
        {
            entity.ToTable("LessonPlans");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Objectives).HasMaxLength(4000).IsRequired();
            entity.HasIndex(e => e.TeachingTopicId);
            entity.HasIndex(e => e.StaffPersonId);
            entity.HasOne<TeachingTopic>().WithMany().HasForeignKey(e => e.TeachingTopicId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonSession>(entity =>
        {
            entity.ToTable("LessonSessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => new { e.ClassId, e.ActualDate, e.PeriodId });
            entity.HasOne<LessonPlan>().WithMany().HasForeignKey(e => e.LessonPlanId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonSessionOutcomeCoverage>(entity =>
        {
            entity.ToTable("LessonSessionOutcomeCoverages");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.LessonSessionId, e.LearningOutcomeId }).IsUnique();
            entity.HasOne<LessonSession>().WithMany().HasForeignKey(e => e.LessonSessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResourceType>(entity =>
        {
            entity.ToTable("ResourceTypes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(LearningDeliverySeedData.ResourceTypes);
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.ToTable("Resources");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TitleEn).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TitleDv).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FileReference).HasMaxLength(1000).IsRequired();
            entity.HasIndex(e => e.TeachingTopicId);
            entity.HasOne<TeachingTopic>().WithMany().HasForeignKey(e => e.TeachingTopicId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ResourceType>().WithMany().HasForeignKey(e => e.ResourceTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenceType>(entity =>
        {
            entity.ToTable("EvidenceTypes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(LearningDeliverySeedData.EvidenceTypes);
        });

        modelBuilder.Entity<AchievementScale>(entity =>
        {
            entity.ToTable("AchievementScales");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<AchievementLevel>(entity =>
        {
            entity.ToTable("AchievementLevels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.AchievementScaleId, e.Code }).IsUnique();
            entity.HasOne<AchievementScale>().WithMany().HasForeignKey(e => e.AchievementScaleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LearningEvidence>(entity =>
        {
            entity.ToTable("LearningEvidences");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => new { e.StudentPersonId, e.LearningOutcomeId });
            entity.HasOne<EvidenceType>().WithMany().HasForeignKey(e => e.EvidenceTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AchievementLevel>().WithMany().HasForeignKey(e => e.AchievementLevelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<LessonSession>().WithMany().HasForeignKey(e => e.LessonSessionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MasteryEvaluation>(entity =>
        {
            entity.ToTable("MasteryEvaluations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentPersonId, e.LearningOutcomeId, e.RecordedAtUtc });
            entity.HasOne<AchievementScale>().WithMany().HasForeignKey(e => e.AchievementScaleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AchievementLevel>().WithMany().HasForeignKey(e => e.AchievementLevelId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KeyCompetency>(entity =>
        {
            entity.ToTable("KeyCompetencies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.NameEn).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameDv).HasMaxLength(200);
            entity.HasData(LearningDeliverySeedData.KeyCompetencies);
        });

        modelBuilder.Entity<KeyCompetencyIndicator>(entity =>
        {
            entity.ToTable("KeyCompetencyIndicators");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.DescriptionEn).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.DescriptionDv).HasMaxLength(1000).IsRequired();
            entity.HasIndex(e => new { e.KeyCompetencyId, e.KeyStageId });
            entity.HasOne<KeyCompetency>().WithMany().HasForeignKey(e => e.KeyCompetencyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KeyCompetencyEvidence>(entity =>
        {
            entity.ToTable("KeyCompetencyEvidences");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => new { e.StudentPersonId, e.KeyCompetencyIndicatorId });
            entity.HasOne<KeyCompetencyIndicator>().WithMany().HasForeignKey(e => e.KeyCompetencyIndicatorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<EvidenceType>().WithMany().HasForeignKey(e => e.EvidenceTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Homework>(entity =>
        {
            entity.ToTable("Homeworks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TitleEn).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TitleDv).HasMaxLength(200).IsRequired();
            entity.Property(e => e.InstructionsEn).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.InstructionsDv).HasMaxLength(4000).IsRequired();
            entity.HasIndex(e => new { e.ClassId, e.DueDate });
            entity.HasIndex(e => e.TeachingTopicId);
            entity.HasOne<TeachingTopic>().WithMany().HasForeignKey(e => e.TeachingTopicId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<HomeworkSubmission>(entity =>
        {
            entity.ToTable("HomeworkSubmissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubmissionText).HasMaxLength(4000);
            entity.Property(e => e.FileReference).HasMaxLength(1000);
            entity.Property(e => e.FeedbackText).HasMaxLength(2000);
            entity.HasIndex(e => new { e.HomeworkId, e.StudentPersonId }).IsUnique();
            entity.HasOne<Homework>().WithMany().HasForeignKey(e => e.HomeworkId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
