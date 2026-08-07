using HAMS.ReportingAnalyticsAudit.Domain;
using HAMS.ReportingAnalyticsAudit.Domain.Views;
using Microsoft.EntityFrameworkCore;

namespace HAMS.ReportingAnalyticsAudit.Infrastructure;

/// <summary>Owns the "reporting" schema exclusively (build plan §2: one schema per module) — this module's first-ever DbContext (Phase 11).</summary>
public sealed class ReportingAnalyticsAuditDbContext(DbContextOptions<ReportingAnalyticsAuditDbContext> options) : DbContext(options)
{
    public DbSet<ReportCard> ReportCards => Set<ReportCard>();
    public DbSet<ReportCardSubjectResult> ReportCardSubjectResults => Set<ReportCardSubjectResult>();
    public DbSet<ReportCardKeyCompetencySummary> ReportCardKeyCompetencySummaries => Set<ReportCardKeyCompetencySummary>();

    /// <summary>
    /// Keyless query types backed by read-only cross-schema SQL views (Phase 12 — build plan §2's
    /// explicit exception for this module's dashboards/regulatory-reports job). Never written to
    /// through this DbContext; the views themselves are created/dropped by this module's own
    /// migrations, same as any other schema object it owns.
    /// </summary>
    public DbSet<StudentRosterRow> StudentRoster => Set<StudentRosterRow>();
    public DbSet<AttendanceRecordRow> AttendanceRecords => Set<AttendanceRecordRow>();
    public DbSet<InterventionCaseSummaryRow> InterventionCaseSummary => Set<InterventionCaseSummaryRow>();
    public DbSet<PromotionDecisionRow> PromotionDecisions => Set<PromotionDecisionRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reporting");

        modelBuilder.Entity<StudentRosterRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_StudentRoster");
        });

        modelBuilder.Entity<AttendanceRecordRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_AttendanceRecords");
        });

        modelBuilder.Entity<InterventionCaseSummaryRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_InterventionCaseSummary");
        });

        modelBuilder.Entity<PromotionDecisionRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_PromotionDecisions");
        });

        modelBuilder.Entity<ReportCard>(entity =>
        {
            entity.ToTable("ReportCards");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NarrativeEn).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.NarrativeDv).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.NextStepsEn).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.NextStepsDv).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.ApprovalStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => new { e.StudentPersonId, e.EvaluationPeriodId, e.IsCurrent });
            entity.Ignore(e => e.IsImmutable);
        });

        modelBuilder.Entity<ReportCardSubjectResult>(entity =>
        {
            entity.ToTable("ReportCardSubjectResults");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Percentage).HasColumnType("decimal(5,2)");
            entity.HasIndex(e => e.ReportCardId);
            entity.HasOne<ReportCard>().WithMany().HasForeignKey(e => e.ReportCardId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReportCardKeyCompetencySummary>(entity =>
        {
            entity.ToTable("ReportCardKeyCompetencySummaries");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportCardId);
            entity.HasOne<ReportCard>().WithMany().HasForeignKey(e => e.ReportCardId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
