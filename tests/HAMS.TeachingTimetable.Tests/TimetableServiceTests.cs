using HAMS.TeachingTimetable.Application;
using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Tests;

public class TimetableServiceTests
{
    private static readonly TimeOnly Period1Start = new(8, 0);
    private static readonly TimeOnly Period1End = new(8, 40);
    private static readonly TimeOnly Period2Start = new(8, 40);
    private static readonly TimeOnly Period2End = new(9, 20);

    private static TeachingTimetableDbContext CreateContext() => new(
        new DbContextOptionsBuilder<TeachingTimetableDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static TimetableService CreateService(TeachingTimetableDbContext db, params DayOfWeek[] nonWorkingDays)
        => new(db, new FakeSchoolCalendarService(nonWorkingDays), new FakeOrgStructureLookup());

    private static TimetableService CreateService(TeachingTimetableDbContext db, FakeOrgStructureLookup orgStructureLookup)
        => new(db, new FakeSchoolCalendarService(), orgStructureLookup);

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

        var entryId = await service.ScheduleAsync(Guid.NewGuid(), classId, subjectId, assignmentId, academicYearId, DayOfWeek.Monday, Period1Start, Period1End);

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
            service.ScheduleAsync(Guid.NewGuid(), classId, subjectId, assignmentId, academicYearId, DayOfWeek.Friday, Period1Start, Period1End));
    }

    [Fact]
    public async Task ScheduleAsync_rejects_a_class_double_booked_in_an_identical_period()
    {
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();

        var mathsAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var englishAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var service = CreateService(db);

        await service.ScheduleAsync(schoolId, classId, Guid.NewGuid(), mathsAssignmentId, academicYearId, DayOfWeek.Monday, Period1Start, Period1End);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ScheduleAsync(schoolId, classId, Guid.NewGuid(), englishAssignmentId, academicYearId, DayOfWeek.Monday, Period1Start, Period1End));
    }

    [Fact]
    public async Task ScheduleAsync_rejects_a_class_double_booked_in_an_overlapping_but_different_period()
    {
        // The core correctness fix this service exists for: two different (auto-created) Periods
        // that merely overlap in wall-clock time must still be rejected, not just exact-PeriodId
        // matches — the old implementation only ever compared PeriodId equality.
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();

        var firstAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var secondAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var service = CreateService(db);

        await service.ScheduleAsync(schoolId, classId, Guid.NewGuid(), firstAssignmentId, academicYearId, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(8, 40));

        // 08:30-09:10 overlaps the first slot (08:00-08:40) by 10 minutes despite being a distinct
        // time range that would find-or-create its own new Period row.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ScheduleAsync(schoolId, classId, Guid.NewGuid(), secondAssignmentId, academicYearId, DayOfWeek.Monday, new TimeOnly(8, 30), new TimeOnly(9, 10)));
    }

    [Fact]
    public async Task ScheduleAsync_rejects_a_staff_member_double_booked_across_two_different_classes()
    {
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();

        var classAAssignmentId = await SeedAssignmentAsync(db, teacherId, Guid.NewGuid(), Guid.NewGuid(), academicYearId);
        var classBAssignmentId = await SeedAssignmentAsync(db, teacherId, Guid.NewGuid(), Guid.NewGuid(), academicYearId);
        var service = CreateService(db);

        await service.ScheduleAsync(schoolId, Guid.NewGuid(), Guid.NewGuid(), classAAssignmentId, academicYearId, DayOfWeek.Tuesday, Period1Start, Period1End);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ScheduleAsync(schoolId, Guid.NewGuid(), Guid.NewGuid(), classBAssignmentId, academicYearId, DayOfWeek.Tuesday, Period1Start, Period1End));
    }

    [Fact]
    public async Task ScheduleAsync_allows_the_same_class_in_a_different_non_overlapping_period_the_same_day()
    {
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();

        var mathsAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var englishAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var service = CreateService(db);

        await service.ScheduleAsync(schoolId, classId, Guid.NewGuid(), mathsAssignmentId, academicYearId, DayOfWeek.Monday, Period1Start, Period1End);
        var secondEntryId = await service.ScheduleAsync(schoolId, classId, Guid.NewGuid(), englishAssignmentId, academicYearId, DayOfWeek.Monday, Period2Start, Period2End);

        Assert.NotEqual(Guid.Empty, secondEntryId);
    }

    [Fact]
    public async Task ScheduleAsync_reuses_the_same_Period_row_for_an_identical_time_span_at_the_same_school()
    {
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var firstAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), academicYearId);
        var secondAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), academicYearId);
        var service = CreateService(db);

        await service.ScheduleAsync(schoolId, Guid.NewGuid(), Guid.NewGuid(), firstAssignmentId, academicYearId, DayOfWeek.Monday, Period1Start, Period1End);
        await service.ScheduleAsync(schoolId, Guid.NewGuid(), Guid.NewGuid(), secondAssignmentId, academicYearId, DayOfWeek.Tuesday, Period1Start, Period1End);

        var periods = await db.Periods.Where(p => p.SchoolId == schoolId).ToListAsync();
        Assert.Single(periods);
    }

    [Fact]
    public async Task RemoveAsync_deletes_the_entry_and_frees_the_slot()
    {
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();

        var assignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var service = CreateService(db);
        var entryId = await service.ScheduleAsync(schoolId, classId, Guid.NewGuid(), assignmentId, academicYearId, DayOfWeek.Wednesday, Period1Start, Period1End);

        await service.RemoveAsync(entryId);

        var replacementAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var newEntryId = await service.ScheduleAsync(schoolId, classId, Guid.NewGuid(), replacementAssignmentId, academicYearId, DayOfWeek.Wednesday, Period1Start, Period1End);

        Assert.NotEqual(Guid.Empty, newEntryId);
    }

    [Fact]
    public async Task GetEntriesForClassAsync_returns_only_that_class_and_years_entries_ordered_by_day_then_period()
    {
        await using var db = CreateContext();
        var academicYearId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var assignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId);
        var service = CreateService(db);

        var wednesdayEntryId = await service.ScheduleAsync(schoolId, classId, Guid.NewGuid(), assignmentId, academicYearId, DayOfWeek.Wednesday, Period1Start, Period1End);
        var mondayEntryId = await service.ScheduleAsync(schoolId, classId, Guid.NewGuid(), assignmentId, academicYearId, DayOfWeek.Monday, Period1Start, Period1End);
        var otherClassAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), academicYearId);
        await service.ScheduleAsync(schoolId, Guid.NewGuid(), Guid.NewGuid(), otherClassAssignmentId, academicYearId, DayOfWeek.Monday, Period1Start, Period1End); // different class

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

        var orgLookup = new FakeOrgStructureLookup()
            .WithSubject(mathsSubjectId, "Mathematics")
            .WithSubject(englishSubjectId, "English")
            .WithClass(classAId, "Grade 5A")
            .WithClass(classBId, "Grade 6B");
        var service = CreateService(db, orgLookup);

        var mathsAssignmentId = await SeedAssignmentAsync(db, teacherId, mathsSubjectId, classAId, academicYearId);
        var englishAssignmentId = await SeedAssignmentAsync(db, teacherId, englishSubjectId, classBId, academicYearId);
        var otherTeacherAssignmentId = await SeedAssignmentAsync(db, Guid.NewGuid(), mathsSubjectId, classAId, academicYearId);

        await service.ScheduleAsync(schoolId, classBId, englishSubjectId, englishAssignmentId, academicYearId, DayOfWeek.Monday, Period2Start, Period2End);
        await service.ScheduleAsync(schoolId, classAId, mathsSubjectId, mathsAssignmentId, academicYearId, DayOfWeek.Monday, Period1Start, Period1End);
        await service.ScheduleAsync(schoolId, classAId, mathsSubjectId, otherTeacherAssignmentId, academicYearId, DayOfWeek.Tuesday, Period1Start, Period1End); // different teacher, not returned

        var entries = await service.GetEntriesForStaffAsync(teacherId, schoolId, academicYearId, new DateOnly(2026, 8, 10));

        Assert.Equal(2, entries.Count);
        Assert.Equal("Mathematics", entries[0].SubjectName);
        Assert.Equal("Grade 5A", entries[0].ClassName);
        Assert.Equal("08:00–08:40", entries[0].PeriodName);
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
        var service = CreateService(db, new FakeOrgStructureLookup());

        var assignmentId = await SeedAssignmentAsync(db, teacherId, subjectId, classId, academicYearId);
        await service.ScheduleAsync(schoolId, classId, subjectId, assignmentId, academicYearId, DayOfWeek.Monday, Period1Start, Period1End);

        var assignment = await db.SubjectTeachingAssignments.FindAsync(assignmentId);
        assignment!.EffectiveTo = new DateOnly(2026, 1, 31); // ended before the asOf date below
        await db.SaveChangesAsync();

        var entries = await service.GetEntriesForStaffAsync(teacherId, schoolId, academicYearId, new DateOnly(2026, 8, 10));

        Assert.Empty(entries);
    }

    [Fact]
    public async Task GetEntriesForSchoolAsync_returns_every_classs_entries_with_resolved_names_and_colors_ordered_by_day_then_time()
    {
        await using var db = CreateContext();
        var schoolId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var mathsSubjectId = Guid.NewGuid();
        var englishSubjectId = Guid.NewGuid();
        var classAId = Guid.NewGuid();
        var classBId = Guid.NewGuid();
        var teacherAId = Guid.NewGuid();
        var teacherBId = Guid.NewGuid();

        var orgLookup = new FakeOrgStructureLookup()
            .WithSubject(mathsSubjectId, "Mathematics")
            .WithSubject(englishSubjectId, "English")
            .WithClass(classAId, "Grade 5A", "#EF4444")
            .WithClass(classBId, "Grade 6B", "#10B981");
        var service = CreateService(db, orgLookup);

        var mathsAssignmentId = await SeedAssignmentAsync(db, teacherAId, mathsSubjectId, classAId, academicYearId);
        var englishAssignmentId = await SeedAssignmentAsync(db, teacherBId, englishSubjectId, classBId, academicYearId);

        await service.ScheduleAsync(schoolId, classBId, englishSubjectId, englishAssignmentId, academicYearId, DayOfWeek.Monday, Period2Start, Period2End);
        await service.ScheduleAsync(schoolId, classAId, mathsSubjectId, mathsAssignmentId, academicYearId, DayOfWeek.Monday, Period1Start, Period1End);

        var entries = await service.GetEntriesForSchoolAsync(schoolId, academicYearId);

        Assert.Equal(2, entries.Count);
        Assert.Equal("Grade 5A", entries[0].ClassName);
        Assert.Equal("#EF4444", entries[0].ColorHex);
        Assert.Equal("Mathematics", entries[0].SubjectName);
        Assert.Equal(teacherAId, entries[0].StaffPersonId);
        Assert.Equal("Grade 6B", entries[1].ClassName);
        Assert.Equal("#10B981", entries[1].ColorHex);
    }
}
