namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// A structural lifecycle, not business/reference data — a genuine C# enum by the same exception
/// as <c>RecordStatus</c>: curriculum coverage is calculated only from <see cref="Completed"/>
/// sessions (LES-FR-012), a rule the code must branch on regardless of storage, and no school
/// would ever want to rename or add to this set.
/// </summary>
public enum LessonSessionStatus
{
    Planned = 0,
    Completed = 1,
    Cancelled = 2,
}
