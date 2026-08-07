using HAMS.TeachingTimetable.Application;
using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Tests;

public class TimetableServiceTests
{
    private static TeachingTimetableDbContext CreateContext() => new(
        new DbContextOptionsBuilder<TeachingTimetableDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static TimetableService CreateService(TeachingTimetableDbContext db, params DayOfWeek[] nonWorkingDays)
        => new(db, new FakeSchoolCalendarService(nonWorkingDays), new FakeOrgStructureLookup());

    private static TimetableService CreateService(TeachingTimetableDbContext db, FakeOrgStructureLookup orgStructureLookup)
        => new(db, new FakeSchoolCalendarService(), orgStructureLookup);

    private static async Task SeedPeriodAsync(TeachingTimetableDbContext db, Guid periodId, string name, TimeOnly start, TimeOnly end)
    {
        db.Periods.Add(new Period { Id = periodId, SchoolId = Guid.NewGuid(), Code = name, Name = name, StartTime = start, EndTime = end });
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> SeedAssignmentAsync(TeachingTimetableDbContext db, Guid staffPersonId, Guid subjectId, Guid classId, Guid academicYearId)
    {
        var role = new AssignmentRole { Id = Guid.NewGuid(), Code = AssignmentRoleCodes.Ordinary, Name = "Ordinary" };
        db.AssignmentRoles.Add(role);
        var assignment = new SubjectTeachingAssignment
        {
            Id = Guid.NewGuid(), StaffPersonId = staffPersonId, SubjectId = subjectId, ClassId = classId,
            AcademicYearId = academicYearId, AssignmentRoleId = role.Id, EffectiveFrom = new DateOnly(2026, 1, 1),
        };
        db.SubjectTeachingAssignments.Add(assignment);
        await db.SaveChangesAsync();
        return assignment.Id;
    }

    [Fact]
    public async Task ScheduleAsync_succeeds_for_a_free_slot_on_a_working_day()
    {
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var assignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), subjectId, classId, academicYearId);
        var service = CreateService(db);

        var entryId = await service.ScheduleAsync(Guid.NewGuid(), classId, subjectId, assignmentId, academicYearId, DayOfWeek.Monday, Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, entryId);
    }

    [Fact]
    public async Task ScheduleAsync_rejects_a_day_that_is_not_a_configured_working_day()
    {
        // Per the Maldivian default (Sunday-Thursday), Friday/Saturday are the weekend — the
        // service must consult the configured working days, never assume Monday-Friday.
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var assignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), subjectId, classId, academicYearId);
        var service = CreateService(db, DayOfWeek.Friday, DayOfWeek.Saturday);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ScheduleAsync(Guid.NewGuid(), classId, subjectId, assignmentId, academicYearId, DayOfWeek.Friday, Guid.NewGuid()));
    }

    [Fact]
    public async Task ScheduleAsync_rejects_a_class_double_booked_in_the_same_slot()
    {
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var mathsAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var englishAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var service = CreateService(db);

        await service.ScheduleAsync(Guid.NewGuid(), classId, Guid.NewGuid(), mathsAssignmentId, academicYearId, DayOfWeek.Monday, periodId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ScheduleAsync(Guid.NewGuid(), classId, Guid.NewGuid(), englishAssignmentId, academicYearId, DayOfWeek.Monday, periodId));
    }

    [Fact]
    public async Task ScheduleAsync_rejects_a_staff_member_double_booked_across_two_different_classes()
    {
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var classAAssignmentId = await SeedAssignmentAsync(db, teacherId, Guid.NewGuid(), Guid.NewGuid(), academicYearId);
        var classBAssignmentId = await SeedAssignmentAsync(db, teacherId, Guid.NewGuid(), Guid.NewGuid(), academicYearId);
        var service = CreateService(db);

        await service.ScheduleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), classAAssignmentId, academicYearId, DayOfWeek.Tuesday, periodId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ScheduleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), classBAssignmentId, academicYearId, DayOfWeek.Tuesday, periodId));
    }

    [Fact]
    public async Task ScheduleAsync_allows_the_same_class_in_a_different_period_the_same_day()
    {
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        var mathsAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var englishAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var service = CreateService(db);

        await service.ScheduleAsync(Guid.NewGuid(), classId, Guid.NewGuid(), mathsAssignmentId, academicYearId, DayOfWeek.Monday, Guid.NewGuid());
        var secondEntryId = await service.ScheduleAsync(Guid.NewGuid(), classId, Guid.NewGuid(), englishAssignmentId, academicYearId, DayOfWeek.Monday, Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, secondEntryId);
    }

    [Fact]
    public async Task RemoveAsync_deletes_the_entry_and_frees_the_slot()
    {
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var assignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var service = CreateService(db);
        var entryId = await service.ScheduleAsync(Guid.NewGuid(), classId, Guid.NewGuid(), assignmentId, academicYearId, DayOfWeek.Wednesday, periodId);

        await service.RemoveAsync(entryId);

        var replacementAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var newEntryId = await service.ScheduleAsync(Guid.NewGuid(), classId, Guid.NewGuid(), replacementAssignmentId, academicYearId, DayOfWeek.Wednesday, periodId);

        Assert.NotEqual(Guid.Empty, newEntryId);
    }

    [Fact]
    public async Task GetEntriesForClassAsync_returns_only_that_class_and_years_entries_ordered_by_day_then_period()
    {
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var assignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var service = CreateService(db);

        var wednesdayEntryId = await service.ScheduleAsync(Guid.NewGuid(), classId, Guid.NewGuid(), assignmentId, academicYearId, DayOfWeek.Wednesday, Guid.NewGuid());
        var mondayEntryId = await service.ScheduleAsync(Guid.NewGuid(), classId, Guid.NewGuid(), assignmentId, academicYearId, DayOfWeek.Monday, Guid.NewGuid());
        await service.ScheduleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), academicYearId), academicYearId, DayOfWeek.Monday, Guid.NewGuid()); // different class

        var entries = await service.GetEntriesForClassAsync(classId, academicYearId);

        Assert.Equal([mondayEntryId, wednesdayEntryId], entries.Select(e => e.Id));
    }

    [Fact]
    public async Task GetEntriesForStaffAsync_returns_only_this_staff_members_entries_with_resolved_names_ordered_by_day_then_time()
    {
        await using var db = CreateContext();
        var schoolId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var mathsSubjectId = Guid.NewGuid();
        var englishSubjectId = Guid.NewGuid();
        var classAId = Guid.NewGuid();
        var classBId = Guid.NewGuid();
        var period1Id = Guid.NewGuid();
        var period2Id = Guid.NewGuid();
        await SeedPeriodAsync(db, period1Id, "Period 1", new TimeOnly(8, 0), new TimeOnly(8, 40));
        await SeedPeriodAsync(db, period2Id, "Period 2", new TimeOnly(8, 40), new TimeOnly(9, 20));

        var orgLookup = new FakeOrgStructureLookup()
            .WithSubject(mathsSubjectId, "Mathematics")
            .WithSubject(englishSubjectId, "English")
            .WithClass(classAId, "Grade 5A")
            .WithClass(classBId, "Grade 6B");
        var service = CreateService(db, orgLookup);

        var mathsAssignmentId = await SeedAssignmentAsync(db, teacherId, mathsSubjectId, classAId, academicYearId);
        var englishAssignmentId = await SeedAssignmentAsync(db, teacherId, englishSubjectId, classBId, academicYearId);
        var otherTeacherAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), mathsSubjectId, classAId, academicYearId);

        await service.ScheduleAsync(schoolId, classBId, englishSubjectId, englishAssignmentId, academicYearId, DayOfWeek.Monday, period2Id);
        await service.ScheduleAsync(schoolId, classAId, mathsSubjectId, mathsAssignmentId, academicYearId, DayOfWeek.Monday, period1Id);
        await service.ScheduleAsync(schoolId, classAId, mathsSubjectId, otherTeacherAssignmentId, academicYearId, DayOfWeek.Tuesday, period1Id); // different teacher, not returned

        var entries = await service.GetEntriesForStaffAsync(teacherId, schoolId, academicYearId, new DateOnly(2026, 8, 10));

        Assert.Equal(2, entries.Count);
        Assert.Equal("Mathematics", entries[0].SubjectName);
        Assert.Equal("Grade 5A", entries[0].ClassName);
        Assert.Equal("Period 1", entries[0].PeriodName);
        Assert.Equal("English", entries[1].SubjectName);
        Assert.Equal("Grade 6B", entries[1].ClassName);
    }

    [Fact]
    public async Task GetEntriesForStaffAsync_excludes_an_assignment_that_is_no_longer_effective()
    {
        await using var db = CreateContext();
        var schoolId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        await SeedPeriodAsync(db, periodId, "Period 1", new TimeOnly(8, 0), new TimeOnly(8, 40));
        var service = CreateService(db, new FakeOrgStructureLookup());

        var assignmentId = await SeedAssignmentAsync(db, teacherId, subjectId, classId, academicYearId);
        await service.ScheduleAsync(schoolId, classId, subjectId, assignmentId, academicYearId, DayOfWeek.Monday, periodId);

        var assignment = await db.SubjectTeachingAssignments.FindAsync(assignmentId);
        assignment!.EffectiveTo = new DateOnly(2026, 1, 31); // ended before the asOf date below
        await db.SaveChangesAsync();

        var entries = await service.GetEntriesForStaffAsync(teacherId, schoolId, academicYearId, new DateOnly(2026, 8, 10));

        Assert.Empty(entries);
    }
}
