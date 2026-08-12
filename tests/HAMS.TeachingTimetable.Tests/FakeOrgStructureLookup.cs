using HAMS.OrgCurriculum.Application;

namespace HAMS.TeachingTimetable.Tests;

/// <summary>Resolves whatever Subject/Class fixtures a test registers — GetEntriesForStaffAsync's own tests need real names to assert against, unlike the conflict-check tests which never touch this dependency.</summary>
internal sealed class FakeOrgStructureLookup : IOrgStructureLookup
{
    private readonly Dictionary<Guid, string> _subjectNames = [];
    private readonly Dictionary<Guid, string> _classNames = [];
    private readonly Dictionary<Guid, string> _classColors = [];
    private readonly Dictionary<Guid, List<Guid>> _classGradeIds = [];
    private readonly List<GradeOption> _grades = [];

    public FakeOrgStructureLookup WithSubject(Guid id, string name)
    {
        _subjectNames[id] = name;
        return this;
    }

    public FakeOrgStructureLookup WithClass(Guid id, string name, string colorHex = "#3B82F6")
    {
        _classNames[id] = name;
        _classColors[id] = colorHex;
        return this;
    }

    public FakeOrgStructureLookup WithClassGrades(Guid classId, params Guid[] gradeIds)
    {
        _classGradeIds[classId] = [.. gradeIds];
        return this;
    }

    public FakeOrgStructureLookup WithGradesForSchool(params GradeOption[] grades)
    {
        _grades.AddRange(grades);
        return this;
    }

    public Task<IReadOnlyList<SchoolOption>> GetSchoolsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SchoolOption>>([]);

    public Task<IReadOnlyList<AcademicYearOption>> GetAcademicYearsAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AcademicYearOption>>([]);

    public Task<IReadOnlyList<GradeOption>> GetGradesAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GradeOption>>(_grades);

    public Task<IReadOnlyList<ClassOption>> GetClassesAsync(Guid academicYearId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ClassOption>>([.. _classNames.Select(kv => new ClassOption(kv.Key, kv.Key.ToString(), kv.Value, _classColors.GetValueOrDefault(kv.Key, "#3B82F6")))]);

    public Task<IReadOnlyList<SubjectOption>> GetSubjectsAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SubjectOption>>([.. _subjectNames.Select(kv => new SubjectOption(kv.Key, kv.Key.ToString(), kv.Value))]);

    public Task<IReadOnlyList<TermOption>> GetTermsAsync(Guid academicYearId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TermOption>>([]);

    public Task<IReadOnlyList<Guid>> GetClassGradeIdsAsync(Guid classId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(_classGradeIds.TryGetValue(classId, out var gradeIds) ? gradeIds : []);

    public Task<Guid?> GetClassSchoolIdAsync(Guid classId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(null);

    public Task<Guid?> GetGradeSchoolIdAsync(Guid gradeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(null);

    public Task<Guid?> GetClassAcademicYearIdAsync(Guid classId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(null);
}
