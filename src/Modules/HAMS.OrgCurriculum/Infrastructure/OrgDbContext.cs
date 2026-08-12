using HAMS.OrgCurriculum.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Infrastructure;

/// <summary>Owns the "org" schema exclusively (build plan §2: one schema per module).</summary>
public sealed class OrgDbContext(DbContextOptions<OrgDbContext> options) : DbContext(options)
{
    public DbSet<School> Schools => Set<School>();
    public DbSet<Campus> Campuses => Set<Campus>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<Phase> Phases => Set<Phase>();
    public DbSet<KeyStage> KeyStages => Set<KeyStage>();
    public DbSet<EvaluationModel> EvaluationModels => Set<EvaluationModel>();
    public DbSet<KeyStagePolicy> KeyStagePolicies => Set<KeyStagePolicy>();
    public DbSet<GradeKeyStageAssignment> GradeKeyStageAssignments => Set<GradeKeyStageAssignment>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<ClassGrade> ClassGrades => Set<ClassGrade>();

    public DbSet<CurriculumFramework> CurriculumFrameworks => Set<CurriculumFramework>();
    public DbSet<LearningArea> LearningAreas => Set<LearningArea>();
    public DbSet<DeliveryMode> DeliveryModes => Set<DeliveryMode>();
    public DbSet<MediumOfInstruction> MediumsOfInstruction => Set<MediumOfInstruction>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Syllabus> Syllabuses => Set<Syllabus>();
    public DbSet<SyllabusGradeApplicability> SyllabusGradeApplicabilities => Set<SyllabusGradeApplicability>();
    public DbSet<Strand> Strands => Set<Strand>();
    public DbSet<SubStrand> SubStrands => Set<SubStrand>();
    public DbSet<LearningOutcome> LearningOutcomes => Set<LearningOutcome>();
    public DbSet<LearningOutcomePrerequisite> LearningOutcomePrerequisites => Set<LearningOutcomePrerequisite>();
    public DbSet<Indicator> Indicators => Set<Indicator>();

    public DbSet<WorkingDay> WorkingDays => Set<WorkingDay>();
    public DbSet<HolidayType> HolidayTypes => Set<HolidayType>();
    public DbSet<Holiday> Holidays => Set<Holiday>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("org");

        modelBuilder.Entity<School>(entity =>
        {
            entity.ToTable("Schools");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Campus>(entity =>
        {
            entity.ToTable("Campuses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.SchoolId, e.Code }).IsUnique();
            entity.HasOne<School>().WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AcademicYear>(entity =>
        {
            entity.ToTable("AcademicYears");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.SchoolId, e.Code }).IsUnique();
            entity.HasOne<School>().WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Term>(entity =>
        {
            entity.ToTable("Terms");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.AcademicYearId, e.Code }).IsUnique();
            entity.HasOne<AcademicYear>().WithMany().HasForeignKey(e => e.AcademicYearId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Phase>(entity =>
        {
            entity.ToTable("Phases");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.SchoolId, e.Code }).IsUnique();
            entity.HasOne<School>().WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KeyStage>(entity =>
        {
            entity.ToTable("KeyStages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.SchoolId, e.Code }).IsUnique();
            entity.HasOne<School>().WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Phase>().WithMany().HasForeignKey(e => e.PhaseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvaluationModel>(entity =>
        {
            entity.ToTable("EvaluationModels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasData(OrgSeedData.EvaluationModels);
        });

        modelBuilder.Entity<KeyStagePolicy>(entity =>
        {
            entity.ToTable("KeyStagePolicies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            // The lookup every evaluation/result row will resolve through — one current row per
            // (KeyStageId, AcademicYearId), enforced at the application level by the versioning
            // discipline (a new version supersedes the old, it never has two "current" rows).
            entity.HasIndex(e => new { e.KeyStageId, e.AcademicYearId, e.IsCurrent });
            entity.HasOne<KeyStage>().WithMany().HasForeignKey(e => e.KeyStageId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AcademicYear>().WithMany().HasForeignKey(e => e.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<EvaluationModel>().WithMany().HasForeignKey(e => e.EvaluationModelId).OnDelete(DeleteBehavior.Restrict);
            entity.Ignore(e => e.IsImmutable);
        });

        modelBuilder.Entity<GradeKeyStageAssignment>(entity =>
        {
            entity.ToTable("GradeKeyStageAssignments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.GradeId, e.AcademicYearId, e.EffectiveFrom, e.EffectiveTo });
            entity.HasOne<Grade>().WithMany().HasForeignKey(e => e.GradeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<KeyStage>().WithMany().HasForeignKey(e => e.KeyStageId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AcademicYear>().WithMany().HasForeignKey(e => e.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.ToTable("Grades");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.SchoolId, e.Code }).IsUnique();
            entity.HasOne<School>().WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.Restrict);
            // A real self-reference within the same schema (unlike every cross-module Guid in this
            // codebase) — a genuine FK is appropriate here since both rows live in the same table.
            entity.HasOne<Grade>().WithMany().HasForeignKey(e => e.NextGradeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.ToTable("Classes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ColorHex).HasMaxLength(7).IsRequired();
            entity.HasIndex(e => new { e.AcademicYearId, e.Code }).IsUnique();
            entity.HasOne<School>().WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Campus>().WithMany().HasForeignKey(e => e.CampusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AcademicYear>().WithMany().HasForeignKey(e => e.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClassGrade>(entity =>
        {
            entity.ToTable("ClassGrades");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ClassId, e.GradeId }).IsUnique();
            entity.HasOne<Class>().WithMany().HasForeignKey(e => e.ClassId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Grade>().WithMany().HasForeignKey(e => e.GradeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CurriculumFramework>(entity =>
        {
            entity.ToTable("CurriculumFrameworks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasData(OrgSeedData.CurriculumFrameworks);
        });

        modelBuilder.Entity<LearningArea>(entity =>
        {
            entity.ToTable("LearningAreas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasOne<CurriculumFramework>().WithMany().HasForeignKey(e => e.CurriculumFrameworkId).OnDelete(DeleteBehavior.Restrict);
            entity.HasData(OrgSeedData.LearningAreas);
        });

        modelBuilder.Entity<DeliveryMode>(entity =>
        {
            entity.ToTable("DeliveryModes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(OrgSeedData.DeliveryModes);
        });

        modelBuilder.Entity<MediumOfInstruction>(entity =>
        {
            entity.ToTable("MediumsOfInstruction");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(OrgSeedData.MediumsOfInstruction);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.ToTable("Subjects");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.SchoolId, e.Code }).IsUnique();
            entity.HasOne<School>().WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<LearningArea>().WithMany().HasForeignKey(e => e.LearningAreaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DeliveryMode>().WithMany().HasForeignKey(e => e.DeliveryModeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<MediumOfInstruction>().WithMany().HasForeignKey(e => e.DefaultMediumOfInstructionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Syllabus>(entity =>
        {
            entity.ToTable("Syllabuses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => new { e.SubjectId, e.IsCurrent });
            entity.HasOne<Subject>().WithMany().HasForeignKey(e => e.SubjectId).OnDelete(DeleteBehavior.Restrict);
            entity.Ignore(e => e.IsImmutable);
        });

        modelBuilder.Entity<SyllabusGradeApplicability>(entity =>
        {
            entity.ToTable("SyllabusGradeApplicabilities");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SyllabusId, e.GradeId }).IsUnique();
            entity.HasOne<Syllabus>().WithMany().HasForeignKey(e => e.SyllabusId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Grade>().WithMany().HasForeignKey(e => e.GradeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Strand>(entity =>
        {
            entity.ToTable("Strands");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.SyllabusId, e.Code }).IsUnique();
            entity.HasOne<Syllabus>().WithMany().HasForeignKey(e => e.SyllabusId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubStrand>(entity =>
        {
            entity.ToTable("SubStrands");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.StrandId, e.Code }).IsUnique();
            entity.HasOne<Strand>().WithMany().HasForeignKey(e => e.StrandId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LearningOutcome>(entity =>
        {
            entity.ToTable("LearningOutcomes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.SubStrandId, e.Code }).IsUnique();
            entity.HasOne<SubStrand>().WithMany().HasForeignKey(e => e.SubStrandId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LearningOutcomePrerequisite>(entity =>
        {
            entity.ToTable("LearningOutcomePrerequisites");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.LearningOutcomeId, e.PrerequisiteLearningOutcomeId }).IsUnique();
            entity.HasOne<LearningOutcome>().WithMany().HasForeignKey(e => e.LearningOutcomeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<LearningOutcome>().WithMany().HasForeignKey(e => e.PrerequisiteLearningOutcomeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Indicator>(entity =>
        {
            entity.ToTable("Indicators");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.LearningOutcomeId, e.Code }).IsUnique();
            entity.HasOne<LearningOutcome>().WithMany().HasForeignKey(e => e.LearningOutcomeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkingDay>(entity =>
        {
            entity.ToTable("WorkingDays");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DayOfWeek).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => new { e.SchoolId, e.DayOfWeek }).IsUnique();
            entity.HasOne<School>().WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HolidayType>(entity =>
        {
            entity.ToTable("HolidayTypes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(OrgSeedData.HolidayTypes);
        });

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.ToTable("Holidays");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NameEn).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameDv).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.SchoolId, e.Date }).IsUnique();
            entity.HasOne<School>().WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<HolidayType>().WithMany().HasForeignKey(e => e.HolidayTypeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
