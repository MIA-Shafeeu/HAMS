namespace HAMS.OrgCurriculum.Application;

/// <summary>The one small read a report card (Phase 11, ReportingAnalyticsAudit) needs to label a subject result by name — nothing previously exposed <c>Subject.Name</c> outside this module.</summary>
public interface ISubjectLookup
{
    /// <returns>Null if the subject doesn't exist.</returns>
    Task<string?> GetNameAsync(Guid subjectId, CancellationToken cancellationToken = default);
}
