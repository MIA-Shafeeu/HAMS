using HAMS.OrgCurriculum.Application;
using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Application;

internal sealed class TimetableService(
    TeachingTimetableDbContext dbContext, ISchoolCalendarService schoolCalendar, IOrgStructureLookup orgStructureLookup)
    : ITimetableService
{
    public async Task<Guid> ScheduleAsync(
        Guid schoolId, Guid classId, Guid subjectId, Guid teachingAssignmentId, Guid academicYearId, DayOfWeek dayOfWeek, Guid periodId,
        CancellationToken cancellationToken = default)
    {
        if (!await schoolCalendar.IsWorkingDayOfWeekAsync(schoolId, dayOfWeek, cancellationToken))
        {
            throw new InvalidOperationException($"{dayOfWeek} is not a configured working day for this school.");
        }

        var assignment = await dbContext.SubjectTeachingAssignments.FindAsync([teachingAssignmentId], cancellationToken)
            ?? throw new InvalidOperationException("Teaching assignment not found.");

        var classConflict = await dbContext.TimetableEntries.AnyAsync(
            e => e.ClassId == classId && e.AcademicYearId == academicYearId && e.DayOfWeek == dayOfWeek && e.PeriodId == periodId,
            cancellationToken);
        if (classConflict)
        {
            throw new InvalidOperationException("This class already has a different subject scheduled in that period.");
        }

        var staffConflict = await dbContext.TimetableEntries
            .Where(e => e.AcademicYearId == academicYearId && e.DayOfWeek == dayOfWeek && e.PeriodId == periodId)
            .Join(dbContext.SubjectTeachingAssignments, e => e.TeachingAssignmentId, a => a.Id, (e, a) => a.StaffPersonId)
            .AnyAsync(staffPersonId => staffPersonId == assignment.StaffPersonId, cancellationToken);
        if (staffConflict)
        {
            throw new InvalidOperationException("This staff member is already teaching a different class in that period.");
        }

        var entry = new TimetableEntry
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            SubjectId = subjectId,
            TeachingAssignmentId = teachingAssignmentId,
            AcademicYearId = academicYearId,
            DayOfWeek = dayOfWeek,
            PeriodId = periodId,
        };
        dbContext.TimetableEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entry.Id;
    }

    public async Task RemoveAsync(Guid timetableEntryId, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.TimetableEntries.FindAsync([timetableEntryId], cancellationToken)
            ?? throw new InvalidOperationException("Timetable entry not found.");

        dbContext.TimetableEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimetableEntry>> GetEntriesForClassAsync(Guid classId, Guid academicYearId, CancellationToken cancellationToken = default) =>
        await dbContext.TimetableEntries
            .Where(e => e.ClassId == classId && e.AcademicYearId == academicYearId)
            .OrderBy(e => e.DayOfWeek).ThenBy(e => e.PeriodId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StaffTimetableEntry>> GetEntriesForStaffAsync(
        Guid staffPersonId, Guid schoolId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        // Order on the raw joined columns before projecting into the final record — ordering by a
        // property of an already-projected DTO doesn't translate against SQL Server.
        var raw = await dbContext.TimetableEntries
            .Where(e => e.AcademicYearId == academicYearId)
            .Join(
                dbContext.SubjectTeachingAssignments.Where(a =>
                    a.StaffPersonId == staffPersonId && a.EffectiveFrom <= asOf && (a.EffectiveTo == null || a.EffectiveTo >= asOf)),
                e => e.TeachingAssignmentId, a => a.Id, (e, a) => e)
            .Join(dbContext.Periods, e => e.PeriodId, p => p.Id, (e, p) => new
            {
                e.Id,
                e.SubjectId,
                e.ClassId,
                e.DayOfWeek,
                PeriodName = p.Name,
                p.StartTime,
                p.EndTime,
            })
            .OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        var subjectNames = (await orgStructureLookup.GetSubjectsAsync(schoolId, cancellationToken)).ToDictionary(s => s.Id, s => s.Name);
        var classNames = (await orgStructureLookup.GetClassesAsync(academicYearId, cancellationToken)).ToDictionary(c => c.Id, c => c.Name);

        return raw
            .Select(x => new StaffTimetableEntry(
                x.Id,
                x.ClassId,
                subjectNames.GetValueOrDefault(x.SubjectId, "(unknown subject)"),
                classNames.GetValueOrDefault(x.ClassId, "(unknown class)"),
                x.DayOfWeek,
                x.PeriodName,
                x.StartTime,
                x.EndTime))
            .ToList();
    }
}
