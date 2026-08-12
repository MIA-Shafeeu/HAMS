using System.Data;
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
        Guid schoolId, Guid classId, Guid subjectId, Guid teachingAssignmentId, Guid academicYearId, DayOfWeek dayOfWeek,
        TimeOnly startTime, TimeOnly endTime, CancellationToken cancellationToken = default)
    {
        if (!await schoolCalendar.IsWorkingDayOfWeekAsync(schoolId, dayOfWeek, cancellationToken))
        {
            throw new InvalidOperationException($"{dayOfWeek} is not a configured working day for this school.");
        }

        var assignment = await dbContext.SubjectTeachingAssignments.FindAsync([teachingAssignmentId], cancellationToken)
            ?? throw new InvalidOperationException("Teaching assignment not found.");

        // Serializable, not just a narrower catch/retry around the Period insert: once two
        // different (but overlapping) auto-created Periods are possible for the same class/day,
        // the DB-level unique index on TimetableEntry(ClassId, AcademicYearId, DayOfWeek, PeriodId)
        // stops being a TOCTOU backstop for that case — only a transaction spanning the whole
        // find-or-create-Period + both conflict checks + insert closes it. This is a low-frequency,
        // human-paced admin write path, so the isolation cost is a non-issue. Guarded by
        // IsRelational() because the in-memory provider used by this module's own unit tests
        // doesn't support transactions at all — every statement below still runs against it, just
        // without the extra atomicity guarantee, which is fine for sequential test execution.
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var period = await dbContext.Periods.SingleOrDefaultAsync(
            p => p.SchoolId == schoolId && p.StartTime == startTime && p.EndTime == endTime, cancellationToken);
        if (period is null)
        {
            period = new Period
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                Code = $"{startTime:HHmm}-{endTime:HHmm}",
                Name = $"{startTime:HH:mm}–{endTime:HH:mm}",
                StartTime = startTime,
                EndTime = endTime,
                DisplayOrder = startTime.Hour * 60 + startTime.Minute,
            };
            dbContext.Periods.Add(period);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var classConflict = await dbContext.TimetableEntries
            .Where(e => e.ClassId == classId && e.AcademicYearId == academicYearId && e.DayOfWeek == dayOfWeek)
            .Join(dbContext.Periods, e => e.PeriodId, p => p.Id, (e, p) => p)
            .AnyAsync(p => p.StartTime < endTime && p.EndTime > startTime, cancellationToken);
        if (classConflict)
        {
            throw new InvalidOperationException("This class already has a different subject scheduled in an overlapping period.");
        }

        var staffConflict = await dbContext.TimetableEntries
            .Where(e => e.AcademicYearId == academicYearId && e.DayOfWeek == dayOfWeek)
            .Join(dbContext.SubjectTeachingAssignments, e => e.TeachingAssignmentId, a => a.Id, (e, a) => new { e.PeriodId, a.StaffPersonId })
            .Where(x => x.StaffPersonId == assignment.StaffPersonId)
            .Join(dbContext.Periods, x => x.PeriodId, p => p.Id, (x, p) => p)
            .AnyAsync(p => p.StartTime < endTime && p.EndTime > startTime, cancellationToken);
        if (staffConflict)
        {
            throw new InvalidOperationException("This staff member is already teaching a different class in an overlapping period.");
        }

        var entry = new TimetableEntry
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            SubjectId = subjectId,
            TeachingAssignmentId = teachingAssignmentId,
            AcademicYearId = academicYearId,
            DayOfWeek = dayOfWeek,
            PeriodId = period.Id,
        };
        dbContext.TimetableEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

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

    public async Task<IReadOnlyList<SchoolTimetableEntry>> GetEntriesForSchoolAsync(Guid schoolId, Guid academicYearId, CancellationToken cancellationToken = default)
    {
        // Order on the raw joined columns before projecting into the final record — ordering by a
        // property of an already-projected DTO doesn't translate against SQL Server (same
        // discipline as GetEntriesForStaffAsync above).
        var raw = await dbContext.TimetableEntries
            .Where(e => e.AcademicYearId == academicYearId)
            .Join(dbContext.SubjectTeachingAssignments, e => e.TeachingAssignmentId, a => a.Id, (e, a) => new { e, a.StaffPersonId })
            .Join(dbContext.Periods, x => x.e.PeriodId, p => p.Id, (x, p) => new
            {
                x.e.Id,
                x.e.ClassId,
                x.e.SubjectId,
                x.StaffPersonId,
                x.e.DayOfWeek,
                p.StartTime,
                p.EndTime,
            })
            .OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        var subjectNames = (await orgStructureLookup.GetSubjectsAsync(schoolId, cancellationToken)).ToDictionary(s => s.Id, s => s.Name);
        var classes = (await orgStructureLookup.GetClassesAsync(academicYearId, cancellationToken)).ToDictionary(c => c.Id, c => c);

        return raw
            .Select(x =>
            {
                var cls = classes.GetValueOrDefault(x.ClassId);
                return new SchoolTimetableEntry(
                    x.Id,
                    x.ClassId,
                    cls?.Name ?? "(unknown class)",
                    cls?.ColorHex ?? "#3B82F6",
                    x.SubjectId,
                    subjectNames.GetValueOrDefault(x.SubjectId, "(unknown subject)"),
                    x.StaffPersonId,
                    x.DayOfWeek,
                    x.StartTime,
                    x.EndTime);
            })
            .ToList();
    }
}
