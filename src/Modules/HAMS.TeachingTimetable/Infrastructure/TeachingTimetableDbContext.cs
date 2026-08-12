using HAMS.TeachingTimetable.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Infrastructure;

/// <summary>Owns the "teaching" schema exclusively (build plan §2: one schema per module).</summary>
public sealed class TeachingTimetableDbContext(DbContextOptions<TeachingTimetableDbContext> options) : DbContext(options)
{
    public DbSet<AssignmentRole> AssignmentRoles => Set<AssignmentRole>();
    public DbSet<SubjectTeachingAssignment> SubjectTeachingAssignments => Set<SubjectTeachingAssignment>();
    public DbSet<ClassTeacherAssignment> ClassTeacherAssignments => Set<ClassTeacherAssignment>();
    public DbSet<LeadingTeacherAssignment> LeadingTeacherAssignments => Set<LeadingTeacherAssignment>();
    public DbSet<SubstitutionRecord> SubstitutionRecords => Set<SubstitutionRecord>();
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("teaching");

        modelBuilder.Entity<AssignmentRole>(entity =>
        {
            entity.ToTable("AssignmentRoles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(TeachingSeedData.AssignmentRoles);
        });

        modelBuilder.Entity<SubjectTeachingAssignment>(entity =>
        {
            entity.ToTable("SubjectTeachingAssignments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StaffPersonId, e.EffectiveFrom, e.EffectiveTo });
            entity.HasIndex(e => new { e.SubjectId, e.ClassId, e.AcademicYearId, e.EffectiveFrom, e.EffectiveTo });
            entity.HasOne<AssignmentRole>().WithMany().HasForeignKey(e => e.AssignmentRoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClassTeacherAssignment>(entity =>
        {
            entity.ToTable("ClassTeacherAssignments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StaffPersonId, e.EffectiveFrom, e.EffectiveTo });
            entity.HasIndex(e => new { e.ClassId, e.AcademicYearId, e.EffectiveFrom, e.EffectiveTo });
        });

        modelBuilder.Entity<LeadingTeacherAssignment>(entity =>
        {
            entity.ToTable("LeadingTeacherAssignments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StaffPersonId, e.EffectiveFrom, e.EffectiveTo });
            entity.HasIndex(e => new { e.SubjectId, e.AcademicYearId, e.EffectiveFrom, e.EffectiveTo });
        });

        modelBuilder.Entity<SubstitutionRecord>(entity =>
        {
            entity.ToTable("SubstitutionRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.HasIndex(e => e.OriginalAssignmentId);
            entity.HasIndex(e => e.GeneratedAssignmentId).IsUnique();
            entity.HasOne<SubjectTeachingAssignment>().WithMany().HasForeignKey(e => e.OriginalAssignmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SubjectTeachingAssignment>().WithMany().HasForeignKey(e => e.GeneratedAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Period>(entity =>
        {
            entity.ToTable("Periods");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.SchoolId, e.Code }).IsUnique();
            // Backstop for the find-or-create-Period logic in ITimetableService.ScheduleAsync —
            // two auto-created periods for the same school and exact time span would otherwise be
            // indistinguishable duplicates.
            entity.HasIndex(e => new { e.SchoolId, e.StartTime, e.EndTime }).IsUnique();
        });

        modelBuilder.Entity<TimetableEntry>(entity =>
        {
            entity.ToTable("TimetableEntries");
            entity.HasKey(e => e.Id);
            // Backstop for "a class can't have two subjects in the same slot" — the staff
            // double-booking check can't be expressed as a plain DB constraint (it needs a join
            // through TeachingAssignmentId to resolve StaffPersonId) so ITimetableService enforces
            // that half of the rule at the application layer instead. Note this only catches EXACT
            // same-Period double-booking — the real, interval-overlap-aware version of this rule
            // (two different but overlapping auto-created Periods) is enforced by wrapping
            // ScheduleAsync in a Serializable transaction, not by a DB constraint.
            entity.HasIndex(e => new { e.ClassId, e.AcademicYearId, e.DayOfWeek, e.PeriodId }).IsUnique();
            entity.HasOne<Period>().WithMany().HasForeignKey(e => e.PeriodId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SubjectTeachingAssignment>().WithMany().HasForeignKey(e => e.TeachingAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
