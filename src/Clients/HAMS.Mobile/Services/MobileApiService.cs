using System.Net.Http.Json;
using HAMS.Mobile.Models;

namespace HAMS.Mobile.Services;

/// <summary>
/// The mobile app's whole read/write surface over the HAMS API (build plan Phase 14 MVP: "my
/// timetable" + attendance marking). Single-school assumption for <see cref="GetSchoolIdAsync"/> —
/// same Ruthless Cut #5 judgment call the WASM portal's own <c>PortalReferenceDataCache</c> already
/// makes, not a new decision.
/// </summary>
public sealed class MobileApiService(HttpClient http)
{
    public async Task<Guid> GetSchoolIdAsync(CancellationToken cancellationToken = default)
    {
        var schools = await http.GetFromJsonAsync<List<SchoolRef>>("api/v1/org/schools", MobileJson.Options, cancellationToken) ?? [];
        return schools.Count > 0 ? schools[0].Id : Guid.Empty;
    }

    public async Task<IReadOnlyList<AcademicYearRef>> GetAcademicYearsAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<List<AcademicYearRef>>($"api/v1/org/academic-years?schoolId={schoolId}", MobileJson.Options, cancellationToken) ?? [];

    public async Task<IReadOnlyList<StaffTimetableEntry>> GetMyTimetableAsync(
        Guid schoolId, Guid academicYearId, CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<List<StaffTimetableEntry>>(
            $"api/v1/teaching/timetable/mine?schoolId={schoolId}&academicYearId={academicYearId}", MobileJson.Options, cancellationToken) ?? [];

    public async Task<IReadOnlyList<ClassRosterEntry>> GetClassRosterAsync(
        Guid classId, DateOnly asOf, CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<List<ClassRosterEntry>>(
            $"api/v1/people/roster/class/{classId}?asOf={asOf:yyyy-MM-dd}", MobileJson.Options, cancellationToken) ?? [];

    public async Task<IReadOnlyList<AttendanceStatusOption>> GetActiveAttendanceStatusesAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await http.GetFromJsonAsync<List<AttendanceStatusOption>>("api/v1/attendance/statuses", MobileJson.Options, cancellationToken) ?? [];
        return statuses.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToList();
    }

    public async Task<(bool Success, string? Error)> MarkAttendanceAsync(MarkDailyAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsJsonAsync("api/v1/attendance/daily", request, MobileJson.Options, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return (false, string.IsNullOrWhiteSpace(error) ? response.ReasonPhrase : error);
    }
}
