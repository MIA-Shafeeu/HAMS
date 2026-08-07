using HAMS.LearningDelivery.Domain;

namespace HAMS.LearningDelivery.Application;

public interface IHomeworkSubmissionService
{
    /// <summary>
    /// Upsert keyed on (homeworkId, studentPersonId) — a resubmission before grading updates the
    /// existing row rather than creating a second one, same convention as <c>AttendanceService</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The homework doesn't exist, or the submission has already been graded.</exception>
    Task<Guid> SubmitAsync(
        Guid homeworkId, Guid studentPersonId, string? submissionText, string? fileReference, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">The submission doesn't exist.</exception>
    Task GradeAsync(Guid submissionId, int? score, string? feedbackText, Guid gradedByPersonId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HomeworkSubmission>> ListForHomeworkAsync(Guid homeworkId, CancellationToken cancellationToken = default);

    Task<HomeworkSubmission?> GetForStudentAsync(Guid homeworkId, Guid studentPersonId, CancellationToken cancellationToken = default);
}
