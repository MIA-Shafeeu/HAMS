using HAMS.Attendance.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Attendance.Infrastructure;

/// <summary>Owns the "attendance" schema exclusively (build plan §2: one schema per module).</summary>
public sealed class AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : DbContext(options)
{
    public DbSet<AttendanceStatus> AttendanceStatuses => Set<AttendanceStatus>();
    public DbSet<DailyAttendanceRecord> DailyAttendanceRecords => Set<DailyAttendanceRecord>();
    public DbSet<LessonAttendanceRecord> LessonAttendanceRecords => Set<LessonAttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("attendance");

        modelBuilder.Entity<AttendanceStatus>(entity =>
        {
            entity.ToTable("AttendanceStatuses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(AttendanceSeedData.AttendanceStatuses);
        });

        modelBuilder.Entity<DailyAttendanceRecord>(entity =>
        {
            entity.ToTable("DailyAttendanceRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => new { e.StudentPersonId, e.Date }).IsUnique();
            entity.HasOne<AttendanceStatus>().WithMany().HasForeignKey(e => e.AttendanceStatusId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LessonAttendanceRecord>(entity =>
        {
            entity.ToTable("LessonAttendanceRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => new { e.StudentPersonId, e.LessonSessionId }).IsUnique();
            entity.HasOne<AttendanceStatus>().WithMany().HasForeignKey(e => e.AttendanceStatusId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
