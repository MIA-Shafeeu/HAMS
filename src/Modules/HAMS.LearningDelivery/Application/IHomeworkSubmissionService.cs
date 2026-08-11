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

    /// <summary>Resolves a submission by its own id — a Staff page re-authorizing <see cref="GradeAsync"/> against the caller's teaching scope needs this to find the submission's owning <c>HomeworkId</c> (and from there its <c>ClassId</c>) before trusting a caller-posted <c>submissionId</c>.</summary>
    Task<HomeworkSubmission?> GetAsync(Guid submissionId, CancellationToken cancellationToken = default);
}
