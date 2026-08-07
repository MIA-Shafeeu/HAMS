using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Domain;
using HAMS.PeopleEnrollment.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.PeopleEnrollment.Tests;

public class StudentEnrollmentServiceTests
{
    private static PeopleDbContext CreateContext() => new(
        new DbContextOptionsBuilder<PeopleDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task SeedOrdinaryEnrollmentTypeAsync(PeopleDbContext db)
    {
        db.EnrollmentTypes.Add(new EnrollmentType { Id = Guid.NewGuid(), Code = EnrollmentTypeCodes.Ordinary, Name = "Ordinary" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task EnrollAsync_succeeds_for_a_students_first_enrolment()
    {
        await using var db = CreateContext();
        await SeedOrdinaryEnrollmentTypeAsync(db);
        var service = new StudentEnrollmentService(db);

        var id = await service.EnrollAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1));

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task EnrollAsync_rejects_a_second_active_ordinary_enrolment_for_the_same_student_and_year()
    {
        await using var db = CreateContext();
        await SeedOrdinaryEnrollmentTypeAsync(db);
        var service = new StudentEnrollmentService(db);
        var studentId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();

        await service.EnrollAsync(studentId, Guid.NewGuid(), Guid.NewGuid(), academicYearId, new DateOnly(2026, 1, 1));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EnrollAsync(studentId, Guid.NewGuid(), Guid.NewGuid(), academicYearId, new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public async Task EnrollAsync_allows_a_new_active_enrolment_once_the_previous_one_has_ended()
    {
        await using var db = CreateContext();
        await SeedOrdinaryEnrollmentTypeAsync(db);
        var service = new StudentEnrollmentService(db);
        var studentId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();

        var firstId = await service.EnrollAsync(studentId, Guid.NewGuid(), Guid.NewGuid(), academicYearId, new DateOnly(2026, 1, 1));
        var first = await db.StudentEnrollments.SingleAsync(e => e.Id == firstId);
        first.EffectiveTo = new DateOnly(2026, 4, 1); // e.g. transferred to a different class mid-year
        await db.SaveChangesAsync();

        var secondId = await service.EnrollAsync(studentId, Guid.NewGuid(), Guid.NewGuid(), academicYearId, new DateOnly(2026, 4, 2));

        Assert.NotEqual(firstId, secondId);
    }

    [Fact]
    public async Task EnrollAsync_allows_the_same_student_active_in_two_different_academic_years()
    {
        await using var db = CreateContext();
        await SeedOrdinaryEnrollmentTypeAsync(db);
        var service = new StudentEnrollmentService(db);
        var studentId = Guid.NewGuid();

        await service.EnrollAsync(studentId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2025, 1, 1));
        var secondId = await service.EnrollAsync(studentId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1));

        Assert.NotEqual(Guid.Empty, secondId);
    }

    private static Person CreateStudentPerson(string nameEn, Guid islandId) => new()
    {
        Id = Guid.NewGuid(), NameEn = nameEn, NameDv = nameEn, DateOfBirth = new DateOnly(2015, 1, 1),
        Address = new Address { IslandId = islandId, RoadEn = "x", RoadDv = "x", HouseNameEn = "x", HouseNameDv = "x" },
    };

    [Fact]
    public async Task GetActiveRosterForClassAsync_returns_only_currently_active_students_in_that_class_with_their_names()
    {
        await using var db = CreateContext();
        await SeedOrdinaryEnrollmentTypeAsync(db);
        var islandId = Guid.NewGuid();
        db.Islands.Add(new Island { Id = islandId, AtollId = Guid.NewGuid(), Code = "TEST_ISLAND", NameEn = "Test Island" });
        var service = new StudentEnrollmentService(db);
        var classId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();

        var inClass = CreateStudentPerson("Ahmed Naseer", islandId);
        var otherClass = CreateStudentPerson("Aishath Nazima", islandId);
        db.People.AddRange(inClass, otherClass);
        db.StudentProfiles.AddRange(
            new StudentProfile { Id = Guid.NewGuid(), PersonId = inClass.Id, AdmissionNumber = "A001", AdmissionDate = new DateOnly(2020, 1, 1) },
            new StudentProfile { Id = Guid.NewGuid(), PersonId = otherClass.Id, AdmissionNumber = "A002", AdmissionDate = new DateOnly(2020, 1, 1) });
        await db.SaveChangesAsync();

        await service.EnrollAsync(inClass.Id, Guid.NewGuid(), classId, academicYearId, new DateOnly(2026, 1, 1));
        await service.EnrollAsync(otherClass.Id, Guid.NewGuid(), Guid.NewGuid(), academicYearId, new DateOnly(2026, 1, 1));

        var roster = await service.GetActiveRosterForClassAsync(classId, new DateOnly(2026, 8, 5));

        var entry = Assert.Single(roster);
        Assert.Equal(inClass.Id, entry.StudentPersonId);
        Assert.Equal("Ahmed Naseer", entry.NameEn);
        Assert.Equal("A001", entry.AdmissionNumber);
    }

    [Fact]
    public async Task GetActiveRosterForClassAsync_excludes_a_student_whose_enrolment_in_that_class_has_ended()
    {
        await using var db = CreateContext();
        await SeedOrdinaryEnrollmentTypeAsync(db);
        var islandId = Guid.NewGuid();
        db.Islands.Add(new Island { Id = islandId, AtollId = Guid.NewGuid(), Code = "TEST_ISLAND", NameEn = "Test Island" });
        var service = new StudentEnrollmentService(db);
        var classId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();

        var student = CreateStudentPerson("Ahmed Naseer", islandId);
        db.People.Add(student);
        db.StudentProfiles.Add(new StudentProfile { Id = Guid.NewGuid(), PersonId = student.Id, AdmissionNumber = "A001", AdmissionDate = new DateOnly(2020, 1, 1) });
        await db.SaveChangesAsync();
        var enrollmentId = await service.EnrollAsync(student.Id, Guid.NewGuid(), classId, academicYearId, new DateOnly(2026, 1, 1));
        await service.EndEnrollmentAsync(enrollmentId, new DateOnly(2026, 6, 1));

        var roster = await service.GetActiveRosterForClassAsync(classId, new DateOnly(2026, 8, 5));

        Assert.Empty(roster);
    }
}
