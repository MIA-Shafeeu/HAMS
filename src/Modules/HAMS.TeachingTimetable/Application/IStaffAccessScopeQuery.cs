namespace HAMS.TeachingTimetable.Application;

/// <summary>
/// What one staff member can see/act on, resolved from their live <c>AccessGrant</c> rows
/// (Platform.Access) plus, where a grant's shape needs it (see <see cref="IStaffAccessScopeQuery"/>),
/// a join back into this module's own teaching assignments or OrgCurriculum's school structure.
/// <see cref="HasUnrestrictedAccess"/> covers System/School Administrator (a wildcard grant with
/// every dimension null) — every list below is meaningless and left empty in that case; callers
/// must check <see cref="HasUnrestrictedAccess"/> first, exactly like <c>ScopeAuthorizationHandler</c>'s
/// own null-means-wildcard rule.
/// </summary>
public sealed record StaffAccessScope(
    bool HasUnrestrictedAccess,
    IReadOnlyList<Guid> SchoolIds,
    IReadOnlyList<Guid> GradeIds,
    IReadOnlyList<Guid> ClassIds)
{
    public bool CanAccessSchool(Guid schoolId) => HasUnrestrictedAccess || SchoolIds.Contains(schoolId);
    public bool CanAccessGrade(Guid gradeId) => HasUnrestrictedAccess || GradeIds.Contains(gradeId);
    public bool CanAccessClass(Guid classId) => HasUnrestrictedAccess || ClassIds.Contains(classId);
}

/// <summary>
/// Resolves a staff member's <c>AccessGrant</c> rows into the concrete Schools/Grades/Classes they
/// can see and act on — the piece that was entirely missing before (every Staff Razor Page's
/// School/Grade/Class picker showed everyone everything, since <c>PlatformAccessPolicies.Scope</c>
/// had zero real consumers anywhere). Lives here rather than in Platform.Access or
/// HAMS.WebHost because resolving a Leading Teacher's grant (Subject-only - "every class currently
/// teaching this subject," the deliberate design choice recorded on
/// <see cref="ILeadingTeacherAssignmentService"/>, since <c>AccessGrant</c> has no Learning-Area
/// dimension to scope Leading Teacher more narrowly) needs this module's own
/// <c>SubjectTeachingAssignment</c> table, and resolving a School-wide grant (Principal/Deputy
/// Principal/School Administrator) needs OrgCurriculum's Grade/Class lists — this module already
/// depends on both, so composing here avoids adding a new cross-module dependency to either one.
/// </summary>
public interface IStaffAccessScopeQuery
{
    /// <param name="schoolId">
    /// Null to resolve ONLY <see cref="StaffAccessScope.SchoolIds"/> (cheap - just the caller's raw
    /// grants, no OrgCurriculum/SubjectTeachingAssignment joins) - the shape a page's very first
    /// dropdown (School) needs before an Academic Year is even chosen. <see cref="StaffAccessScope.GradeIds"/>/
    /// <see cref="StaffAccessScope.ClassIds"/> are left empty in this mode; they're not meaningful
    /// without a specific school+year to resolve them against.
    /// </param>
    /// <param name="academicYearId">Required (and ignored if null) whenever <paramref name="schoolId"/> is supplied - <c>Class</c> is itself academic-year-scoped, so "which classes" can't be answered without one.</param>
    Task<StaffAccessScope> GetScopeAsync(
        Guid personId, DateOnly asOf, Guid? schoolId, Guid? academicYearId, CancellationToken cancellationToken = default);
}
