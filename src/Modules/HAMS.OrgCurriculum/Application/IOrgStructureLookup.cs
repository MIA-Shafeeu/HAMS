namespace HAMS.OrgCurriculum.Application;

public sealed record SchoolOption(Guid Id, string Code, string Name);
public sealed record AcademicYearOption(Guid Id, string Code, string Name, DateOnly StartDate, DateOnly EndDate);
public sealed record GradeOption(Guid Id, string Code, string Name);
public sealed record ClassOption(Guid Id, string Code, string Name, string ColorHex);
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

    /// <summary>Every <c>Grade</c> a <c>Class</c> belongs to (a combined class can span more than one) — the join <c>IStaffAccessScopeQuery</c> (HAMS.TeachingTimetable) needs to turn "which classes can this person reach" into "which grades," since a Class Teacher/Subject Teacher's grant scopes by Class, never Grade, directly.</summary>
    Task<IReadOnlyList<Guid>> GetClassGradeIdsAsync(Guid classId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The <c>School</c> a <c>Class</c> belongs to — needed by a Staff page re-authorizing an action
    /// against an EXISTING record (an incident, a homework submission, ...) that carries a
    /// <c>ClassId</c> but no <c>SchoolId</c> of its own: <c>IStaffAccessScopeQuery.GetScopeAsync</c>
    /// needs a real <c>schoolId</c> to resolve <c>ClassIds</c> correctly, and the record's own
    /// school can't be assumed to be whatever the page's current School dropdown happens to hold.
    /// </summary>
    /// <returns>Null if no such class exists.</returns>
    Task<Guid?> GetClassSchoolIdAsync(Guid classId, CancellationToken cancellationToken = default);

    /// <summary>Same as <see cref="GetClassSchoolIdAsync"/>, for records that carry a <c>GradeId</c> instead of a <c>ClassId</c> (report cards, assessment results, scheme-of-work content).</summary>
    /// <returns>Null if no such grade exists.</returns>
    Task<Guid?> GetGradeSchoolIdAsync(Guid gradeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The <c>AcademicYear</c> a <c>Class</c> belongs to — for the same re-authorization need as
    /// <see cref="GetClassSchoolIdAsync"/>, when the existing record (e.g. a <c>Homework</c>) has no
    /// <c>AcademicYearId</c> of its own either, only a <c>ClassId</c>.
    /// </summary>
    /// <returns>Null if no such class exists.</returns>
    Task<Guid?> GetClassAcademicYearIdAsync(Guid classId, CancellationToken cancellationToken = default);
}
