using System.Net.Http.Json;

namespace HAMS.WebHost.Client.Portal;

/// <summary>
/// Resolves the handful of GUID-keyed reference names (subject, academic year) the portal's read
/// pages need for display — subject/academic-year admin lookups only need any authenticated
/// principal (<c>RequireAuthorization()</c>, no staff-only policy), so a guardian/student token can
/// call them directly. Single-school deployment (build plan §10 Ruthless Cut #5): <see cref="GetSchoolIdAsync"/>
/// just takes the first school returned rather than adding a school picker nothing in this
/// deployment needs yet. Cached per circuit lifetime (this service is scoped) since reference data
/// changes rarely enough that re-fetching per page isn't worth the round trip.
/// </summary>
public sealed class PortalReferenceDataCache(HttpClient http)
{
    private Guid? _schoolId;
    private Dictionary<Guid, string>? _subjectNames;

    public async Task<Guid> GetSchoolIdAsync(CancellationToken cancellationToken = default)
    {
        if (_schoolId is { } cached)
        {
            return cached;
        }

        var schools = await http.GetFromJsonAsync<List<SchoolRef>>("/api/v1/org/schools", PortalJson.Options, cancellationToken) ?? [];
        _schoolId = schools.Count > 0 ? schools[0].Id : Guid.Empty;
        return _schoolId.Value;
    }

    public async Task<string> GetSubjectNameAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        _subjectNames ??= await LoadSubjectsAsync(cancellationToken);
        return _subjectNames.GetValueOrDefault(subjectId, "(unknown subject)");
    }

    public async Task<IReadOnlyList<AcademicYearRef>> GetAcademicYearsAsync(CancellationToken cancellationToken = default)
    {
        var schoolId = await GetSchoolIdAsync(cancellationToken);
        return await http.GetFromJsonAsync<List<AcademicYearRef>>($"/api/v1/org/academic-years?schoolId={schoolId}", PortalJson.Options, cancellationToken) ?? [];
    }

    private async Task<Dictionary<Guid, string>> LoadSubjectsAsync(CancellationToken cancellationToken)
    {
        var schoolId = await GetSchoolIdAsync(cancellationToken);
        var subjects = await http.GetFromJsonAsync<List<SubjectRef>>($"/api/v1/org/subjects?schoolId={schoolId}", PortalJson.Options, cancellationToken) ?? [];
        return subjects.ToDictionary(s => s.Id, s => s.Name);
    }
}
