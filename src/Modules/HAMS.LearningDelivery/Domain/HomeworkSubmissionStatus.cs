namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// A structural lifecycle, not business/reference data — same exemption as <see cref="LessonSessionStatus"/>:
/// "late" is computed once, at submission time, by comparing against <see cref="Homework.DueDate"/>,
/// and no school would ever want to rename or add to this set.
/// </summary>
public enum HomeworkSubmissionStatus
{
    Submitted = 0,
    Late = 1,
    Graded = 2,
}
