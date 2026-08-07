using HAMS.OrgCurriculum.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Application;

internal sealed class OrgStructureLookup(OrgDbContext dbContext) : IOrgStructureLookup
{
    public async Task<IReadOnlyList<SchoolOption>> GetSchoolsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Schools.Where(s => s.IsActive).OrderBy(s => s.Name)
            .Select(s => new SchoolOption(s.Id, s.Code, s.Name)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AcademicYearOption>> GetAcademicYearsAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.AcademicYears.Where(a => a.SchoolId == schoolId).OrderByDescending(a => a.StartDate)
            .Select(a => new AcademicYearOption(a.Id, a.Code, a.Name, a.StartDate, a.EndDate)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GradeOption>> GetGradesAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.Grades.Where(g => g.SchoolId == schoolId && g.IsActive).OrderBy(g => g.DisplayOrder)
            .Select(g => new GradeOption(g.Id, g.Code, g.Name)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClassOption>> GetClassesAsync(Guid academicYearId, CancellationToken cancellationToken = default) =>
        await dbContext.Classes.Where(c => c.AcademicYearId == academicYearId && c.IsActive).OrderBy(c => c.Name)
            .Select(c => new ClassOption(c.Id, c.Code, c.Name)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SubjectOption>> GetSubjectsAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.Subjects.Where(s => s.SchoolId == schoolId && s.IsActive).OrderBy(s => s.DisplayOrder)
            .Select(s => new SubjectOption(s.Id, s.Code, s.Name)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TermOption>> GetTermsAsync(Guid academicYearId, CancellationToken cancellationToken = default) =>
        await dbContext.Terms.Where(t => t.AcademicYearId == academicYearId).OrderBy(t => t.DisplayOrder)
            .Select(t => new TermOption(t.Id, t.Code, t.Name)).ToListAsync(cancellationToken);
}
