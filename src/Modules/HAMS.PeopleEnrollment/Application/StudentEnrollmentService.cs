using HAMS.PeopleEnrollment.Domain;
using HAMS.PeopleEnrollment.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.PeopleEnrollment.Application;

internal sealed class StudentEnrollmentService(PeopleDbContext dbContext) : IStudentEnrollmentService
{
    public async Task<Guid> EnrollAsync(
        Guid studentPersonId, Guid gradeId, Guid classId, Guid academicYearId, DateOnly effectiveFrom,
        CancellationToken cancellationToken = default)
    {
        var ordinaryType = await dbContext.EnrollmentTypes.SingleAsync(t => t.Code == EnrollmentTypeCodes.Ordinary, cancellationToken);

        var hasActiveOrdinaryEnrollment = await dbContext.StudentEnrollments.AnyAsync(
            e => e.StudentPersonId == studentPersonId
                 && e.AcademicYearId == academicYearId
                 && e.EnrollmentTypeId == ordinaryType.Id
                 && e.EffectiveTo == null,
            cancellationToken);

        if (hasActiveOrdinaryEnrollment)
        {
            throw new InvalidOperationException("This student already has an active ordinary enrolment for that academic year.");
        }

        var enrollment = new StudentEnrollment
        {
            Id = Guid.NewGuid(),
            StudentPersonId = studentPersonId,
            GradeId = gradeId,
            ClassId = classId,
            AcademicYearId = academicYearId,
            EnrollmentTypeId = ordinaryType.Id,
            EffectiveFrom = effectiveFrom,
        };

        dbContext.StudentEnrollments.Add(enrollment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return enrollment.Id;
    }

    public async Task<StudentEnrollment?> GetActiveEnrollmentAsync(
        Guid studentPersonId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default)
        => await dbContext.StudentEnrollments
            .Where(e => e.StudentPersonId == studentPersonId && e.AcademicYearId == academicYearId)
            .ActiveAsOf(asOf)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task EndEnrollmentAsync(Guid enrollmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
    {
        var enrollment = await dbContext.StudentEnrollments.FindAsync([enrollmentId], cancellationToken)
            ?? throw new InvalidOperationException("Enrolment not found.");

        if (enrollment.EffectiveTo is not null)
        {
            throw new InvalidOperationException("This enrolment has already been closed.");
        }

        enrollment.EffectiveTo = effectiveTo;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClassRosterEntry>> GetActiveRosterForClassAsync(
        Guid classId, DateOnly asOf, CancellationToken cancellationToken = default)
        => await dbContext.StudentEnrollments
            .Where(e => e.ClassId == classId)
            .ActiveAsOf(asOf)
            .Join(dbContext.People, e => e.StudentPersonId, p => p.Id, (e, p) => new { e, p })
            .Join(dbContext.StudentProfiles, ep => ep.e.StudentPersonId, sp => sp.PersonId, (ep, sp) => new { ep.e.StudentPersonId, ep.p.NameEn, ep.p.NameDv, sp.AdmissionNumber })
            // OrderBy must come BEFORE projecting into ClassRosterEntry — ordering by a property of an
            // already-constructed record (as this used to do) can't be translated by the SQL Server
            // provider ("could not be translated"); it only ever passed InMemory-provider unit tests,
            // never live SQL Server, which is exactly why this needed a live boot-and-check to catch.
            .OrderBy(x => x.NameEn)
            .Select(x => new ClassRosterEntry(x.StudentPersonId, x.NameEn, x.NameDv, x.AdmissionNumber))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClassRosterEntry>> GetActiveRosterForGradeAsync(
        Guid gradeId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default)
        => await dbContext.StudentEnrollments
            .Where(e => e.GradeId == gradeId && e.AcademicYearId == academicYearId)
            .ActiveAsOf(asOf)
            .Join(dbContext.People, e => e.StudentPersonId, p => p.Id, (e, p) => new { e, p })
            .Join(dbContext.StudentProfiles, ep => ep.e.StudentPersonId, sp => sp.PersonId, (ep, sp) => new { ep.e.StudentPersonId, ep.p.NameEn, ep.p.NameDv, sp.AdmissionNumber })
            .OrderBy(x => x.NameEn)
            .Select(x => new ClassRosterEntry(x.StudentPersonId, x.NameEn, x.NameDv, x.AdmissionNumber))
            .ToListAsync(cancellationToken);
}
