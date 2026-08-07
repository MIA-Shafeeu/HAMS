namespace HAMS.OrgCurriculum.Application;

public sealed record SchoolOption(Guid Id, string Code, string Name);
public sealed record AcademicYearOption(Guid Id, string Code, string Name, DateOnly StartDate, DateOnly EndDate);
public sealed record GradeOption(Guid Id, string Code, string Name);
public sealed record ClassOption(Guid Id, string Code, string Name);
public sealed record SubjectOption(Guid Id, string Code, string Name);
public sealed record TermOption(Guid Id, string Code, string Name);

/// <summary>
/// Read-only org-structure lookups for staff-facing UI dropdowns (attendance/homework/assessment/
/// lesson-planning pages all need "which school/year/grade/class/subject" pickers) — the same class
/// of small, additive, cross-consumer read surface as <c>ISubjectLookup</c>/<c>ITeachingTopicQuery</c>
/// from earlier phases, just covering the handful of org entities nothing outside this module could
/// previously list (School/AcademicYear/Grade/Class CRUD has existed only as inline endpoint lambdas
/// in <c>OrgEndpoints.cs</c> since Phase 1 — this is the first DI-injectable read path for any of them).
/// </summary>
public interface IOrgStructureLookup
{
    Task<IReadOnlyList<SchoolOption>> GetSchoolsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicYearOption>> GetAcademicYearsAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GradeOption>> GetGradesAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassOption>> GetClassesAsync(Guid academicYearId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubjectOption>> GetSubjectsAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TermOption>> GetTermsAsync(Guid academicYearId, CancellationToken cancellationToken = default);
}
