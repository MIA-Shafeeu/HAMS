using HAMS.AssessmentEvaluation.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Infrastructure;

/// <summary>Owns the "assessment" schema exclusively (build plan §2: one schema per module).</summary>
public sealed class AssessmentEvaluationDbContext(DbContextOptions<AssessmentEvaluationDbContext> options) : DbContext(options)
{
    public DbSet<AssessmentCategory> AssessmentCategories => Set<AssessmentCategory>();
    public DbSet<ExternalExaminationBoard> ExternalExaminationBoards => Set<ExternalExaminationBoard>();
    public DbSet<SpecialResultState> SpecialResultStates => Set<SpecialResultState>();
    public DbSet<AssessmentScheme> AssessmentSchemes => Set<AssessmentScheme>();
    public DbSet<AssessmentSchemeComponent> AssessmentSchemeComponents => Set<AssessmentSchemeComponent>();
    public DbSet<GradeScale> GradeScales => Set<GradeScale>();
    public DbSet<GradeBand> GradeBands => Set<GradeBand>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();
    public DbSet<ResultAggregationRule> ResultAggregationRules => Set<ResultAggregationRule>();
    public DbSet<EvaluationPeriod> EvaluationPeriods => Set<EvaluationPeriod>();
    public DbSet<KeyStageEvaluation> KeyStageEvaluations => Set<KeyStageEvaluation>();
    public DbSet<PromotionPolicy> PromotionPolicies => Set<PromotionPolicy>();
    public DbSet<PromotionDecision> PromotionDecisions => Set<PromotionDecision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("assessment");

        modelBuilder.Entity<AssessmentCategory>(entity =>
        {
            entity.ToTable("AssessmentCategories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(AssessmentEvaluationSeedData.AssessmentCategories);
        });

        modelBuilder.Entity<ExternalExaminationBoard>(entity =>
        {
            entity.ToTable("ExternalExaminationBoards");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(AssessmentEvaluationSeedData.ExternalExaminationBoards);
        });

        modelBuilder.Entity<SpecialResultState>(entity =>
        {
            entity.ToTable("SpecialResultStates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(AssessmentEvaluationSeedData.SpecialResultStates);
        });

        modelBuilder.Entity<AssessmentScheme>(entity =>
        {
            entity.ToTable("AssessmentSchemes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<AssessmentSchemeComponent>(entity =>
        {
            entity.ToTable("AssessmentSchemeComponents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WeightPercentage).HasColumnType("decimal(5,2)");
            entity.HasIndex(e => new { e.AssessmentSchemeId, e.AssessmentCategoryId }).IsUnique();
            entity.HasOne<AssessmentScheme>().WithMany().HasForeignKey(e => e.AssessmentSchemeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<AssessmentCategory>().WithMany().HasForeignKey(e => e.AssessmentCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ResultAggregationRule>().WithMany().HasForeignKey(e => e.ResultAggregationRuleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ResultAggregationRule>(entity =>
        {
            entity.ToTable("ResultAggregationRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(AssessmentEvaluationSeedData.ResultAggregationRules);
        });

        modelBuilder.Entity<EvaluationPeriod>(entity =>
        {
            entity.ToTable("EvaluationPeriods");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.AcademicYearId, e.Code }).IsUnique();
        });

        modelBuilder.Entity<KeyStageEvaluation>(entity =>
        {
            entity.ToTable("KeyStageEvaluations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OverallPercentage).HasColumnType("decimal(5,2)");
            entity.HasIndex(e => new { e.StudentPersonId, e.SubjectId, e.EvaluationPeriodId, e.RecordedAtUtc });
            entity.HasOne<EvaluationPeriod>().WithMany().HasForeignKey(e => e.EvaluationPeriodId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<GradeBand>().WithMany().HasForeignKey(e => e.OverallGradeBandId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GradeScale>(entity =>
        {
            entity.ToTable("GradeScales");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<GradeBand>(entity =>
        {
            entity.ToTable("GradeBands");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MinPercentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.MaxPercentage).HasColumnType("decimal(5,2)");
            entity.HasIndex(e => new { e.GradeScaleId, e.Code }).IsUnique();
            entity.HasOne<GradeScale>().WithMany().HasForeignKey(e => e.GradeScaleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.ToTable("Assessments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MaxMarks).HasColumnType("decimal(6,2)");
            entity.Property(e => e.ExternalSyllabusCode).HasMaxLength(50);
            entity.HasIndex(e => new { e.SubjectId, e.GradeId, e.TermId });
            entity.HasOne<AssessmentCategory>().WithMany().HasForeignKey(e => e.AssessmentCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ExternalExaminationBoard>().WithMany().HasForeignKey(e => e.ExternalExaminationBoardId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssessmentResult>(entity =>
        {
            entity.ToTable("AssessmentResults");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RawMark).HasColumnType("decimal(6,2)");
            entity.Property(e => e.AdjustedMark).HasColumnType("decimal(6,2)");
            entity.Property(e => e.ModeratedMark).HasColumnType("decimal(6,2)");
            entity.Property(e => e.FinalMark).HasColumnType("decimal(6,2)");
            entity.Property(e => e.ModerationStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.EscalationReason).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => new { e.AssessmentId, e.StudentPersonId, e.IsCurrent });
            entity.HasOne<Assessment>().WithMany().HasForeignKey(e => e.AssessmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SpecialResultState>().WithMany().HasForeignKey(e => e.SpecialResultStateId).OnDelete(DeleteBehavior.Restrict);
            entity.Ignore(e => e.IsImmutable);
        });

        modelBuilder.Entity<PromotionPolicy>(entity =>
        {
            entity.ToTable("PromotionPolicies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(AssessmentEvaluationSeedData.PromotionPolicies);
        });

        modelBuilder.Entity<PromotionDecision>(entity =>
        {
            entity.ToTable("PromotionDecisions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => new { e.StudentPersonId, e.RecordedAtUtc });
        });
    }
}
