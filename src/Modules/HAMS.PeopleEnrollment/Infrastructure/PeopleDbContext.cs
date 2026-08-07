using System.Linq;
using HAMS.PeopleEnrollment.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.PeopleEnrollment.Infrastructure;

/// <summary>Owns the "people" schema exclusively (build plan §2: one schema per module).</summary>
public sealed class PeopleDbContext(DbContextOptions<PeopleDbContext> options) : DbContext(options)
{
    public DbSet<Atoll> Atolls => Set<Atoll>();
    public DbSet<Island> Islands => Set<Island>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<EmploymentStatus> EmploymentStatuses => Set<EmploymentStatus>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<StaffQualification> StaffQualifications => Set<StaffQualification>();
    public DbSet<GuardianProfile> GuardianProfiles => Set<GuardianProfile>();
    public DbSet<RelationshipType> RelationshipTypes => Set<RelationshipType>();
    public DbSet<RestrictionType> RestrictionTypes => Set<RestrictionType>();
    public DbSet<GuardianStudentRelationship> GuardianStudentRelationships => Set<GuardianStudentRelationship>();
    public DbSet<EnrollmentType> EnrollmentTypes => Set<EnrollmentType>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("people");

        modelBuilder.Entity<Atoll>(entity =>
        {
            entity.ToTable("Atolls");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(10).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.NameEn).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameDv).HasMaxLength(100);
            entity.HasData(PeopleSeedData.Atolls);
        });

        modelBuilder.Entity<Island>(entity =>
        {
            entity.ToTable("Islands");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.NameEn).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameDv).HasMaxLength(100);
            entity.HasOne<Atoll>().WithMany().HasForeignKey(e => e.AtollId).OnDelete(DeleteBehavior.Restrict);
            entity.HasData(PeopleSeedData.Islands);
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("People");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NameEn).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameDv).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(320);

            entity.OwnsOne(e => e.Address, address =>
            {
                address.Property(a => a.RoadEn).HasColumnName("Address_RoadEn").HasMaxLength(200).IsRequired();
                address.Property(a => a.RoadDv).HasColumnName("Address_RoadDv").HasMaxLength(200).IsRequired();
                address.Property(a => a.HouseNameEn).HasColumnName("Address_HouseNameEn").HasMaxLength(200).IsRequired();
                address.Property(a => a.HouseNameDv).HasColumnName("Address_HouseNameDv").HasMaxLength(200).IsRequired();
                address.Property(a => a.BuildingEn).HasColumnName("Address_BuildingEn").HasMaxLength(200);
                address.Property(a => a.BuildingDv).HasColumnName("Address_BuildingDv").HasMaxLength(200);
                address.Property(a => a.Floor).HasColumnName("Address_Floor").HasMaxLength(50);
                address.Property(a => a.Apartment).HasColumnName("Address_Apartment").HasMaxLength(50);
                address.Property(a => a.IslandId).HasColumnName("Address_IslandId");
                address.HasOne<Island>().WithMany().HasForeignKey(a => a.IslandId).OnDelete(DeleteBehavior.Restrict);
                address.HasIndex(a => a.IslandId);
            });
        });

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.ToTable("StudentProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AdmissionNumber).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.AdmissionNumber).IsUnique();
            entity.HasIndex(e => e.PersonId).IsUnique();
            entity.HasOne<Person>().WithMany().HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmploymentStatus>(entity =>
        {
            entity.ToTable("EmploymentStatuses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(PeopleSeedData.EmploymentStatuses);
        });

        modelBuilder.Entity<StaffProfile>(entity =>
        {
            entity.ToTable("StaffProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeNumber).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.EmployeeNumber).IsUnique();
            entity.HasIndex(e => e.PersonId).IsUnique();
            entity.HasOne<Person>().WithMany().HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<EmploymentStatus>().WithMany().HasForeignKey(e => e.EmploymentStatusId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StaffQualification>(entity =>
        {
            entity.ToTable("StaffQualifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AwardingInstitution).HasMaxLength(200);
            entity.HasOne<StaffProfile>().WithMany().HasForeignKey(e => e.StaffProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GuardianProfile>(entity =>
        {
            entity.ToTable("GuardianProfiles");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PersonId).IsUnique();
            entity.HasOne<Person>().WithMany().HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RelationshipType>(entity =>
        {
            entity.ToTable("RelationshipTypes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(PeopleSeedData.RelationshipTypes);
        });

        modelBuilder.Entity<RestrictionType>(entity =>
        {
            entity.ToTable("RestrictionTypes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<GuardianStudentRelationship>(entity =>
        {
            entity.ToTable("GuardianStudentRelationships");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VerificationStatus).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => new { e.GuardianPersonId, e.StudentPersonId, e.EffectiveFrom, e.EffectiveTo });
            entity.HasOne<Person>().WithMany().HasForeignKey(e => e.GuardianPersonId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Person>().WithMany().HasForeignKey(e => e.StudentPersonId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RelationshipType>().WithMany().HasForeignKey(e => e.RelationshipTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RestrictionType>().WithMany().HasForeignKey(e => e.RestrictionTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EnrollmentType>(entity =>
        {
            entity.ToTable("EnrollmentTypes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasData(PeopleSeedData.EnrollmentTypes);
        });

        modelBuilder.Entity<StudentEnrollment>(entity =>
        {
            entity.ToTable("StudentEnrollments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentPersonId, e.AcademicYearId, e.EffectiveFrom, e.EffectiveTo });
            entity.HasOne<EnrollmentType>().WithMany().HasForeignKey(e => e.EnrollmentTypeId).OnDelete(DeleteBehavior.Restrict);

            // ORG-FR-017: at most one currently-active Ordinary enrolment per student per academic
            // year. The filter must be a literal, hence embedding the seeded Ordinary type's fixed
            // id directly (see PeopleSeedData) rather than a subquery — SQL Server filtered index
            // predicates don't support subqueries.
            var ordinaryTypeId = PeopleSeedData.EnrollmentTypes.Single(t => t.Code == EnrollmentTypeCodes.Ordinary).Id;
            entity.HasIndex(e => new { e.StudentPersonId, e.AcademicYearId })
                .IsUnique()
                .HasFilter($"[EnrollmentTypeId] = '{ordinaryTypeId}' AND [EffectiveTo] IS NULL")
                .HasDatabaseName("IX_StudentEnrollments_OneActiveOrdinaryPerStudentYear");
        });
    }
}
