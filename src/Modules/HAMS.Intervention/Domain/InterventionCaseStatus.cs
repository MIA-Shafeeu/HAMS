namespace HAMS.Intervention.Domain;

/// <summary>
/// A structural lifecycle state, not business/reference data — same exemption as
/// <c>RecordStatus</c>/<c>LessonSessionStatus</c>: whether a case still needs attention is a
/// code-branching fact regardless of storage, and extending it always needs a new code path anyway.
/// </summary>
public enum InterventionCaseStatus
{
    Open = 0,
    Closed = 1,
}
