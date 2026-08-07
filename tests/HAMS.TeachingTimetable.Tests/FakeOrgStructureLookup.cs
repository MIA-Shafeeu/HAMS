using HAMS.OrgCurriculum.Application;

namespace HAMS.TeachingTimetable.Tests;

/// <summary>Resolves whatever Subject/Class fixtures a test registers — GetEntriesForStaffAsync's own tests need real names to assert against, unlike the conflict-check tests which never touch this dependency.</summary>
internal sealed class FakeOrgStructureLookup : IOrgStructureLookup
{
    private readonly Dictionary<Guid, string> _subjectNames = [];
    private readonly Dictionary<Guid, string> _classNames = [];

    public FakeOrgStructureLookup WithSubject(Guid id, string name)
    {
        _subjectNames[id] = name;
        return this;
    }

    public FakeOrgStructureLookup WithClass(Guid id, string name)
    {
        _classNames[id] = name;
        return this;
    }

    public Task<IReadOnlyList<SchoolOption>> GetSchoolsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SchoolOption>>([]);

    public Task<IReadOnlyList<AcademicYearOption>> GetAcademicYearsAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AcademicYearOption>>([]);

    public Task<IReadOnlyList<GradeOption>> GetGradesAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GradeOption>>([]);

    public Task<IReadOnlyList<ClassOption>> GetClassesAsync(Guid academicYearId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ClassOption>>([.. _classNames.Select(kv => new ClassOption(kv.Key, kv.Key.ToString(), kv.Value))]);

    public Task<IReadOnlyList<SubjectOption>> GetSubjectsAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SubjectOption>>([.. _subjectNames.Select(kv => new SubjectOption(kv.Key, kv.Key.ToString(), kv.Value))]);

    public Task<IReadOnlyList<TermOption>> GetTermsAsync(Guid academicYearId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TermOption>>([]);
}
