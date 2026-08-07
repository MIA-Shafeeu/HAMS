namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// A student's submission against one <see cref="Homework"/> — upsert semantics keyed on
/// (<see cref="HomeworkId"/>, <see cref="StudentPersonId"/>), the same "find-or-create by natural
/// key" convention <c>AttendanceService</c> established (a resubmission before grading updates the
/// same row rather than creating a second one). <see cref="FeedbackText"/> is deliberately
/// single-language, like <see cref="LessonPlan.Objectives"/> — a teacher's own quick note on one
/// submission, not an official bilingual artifact a guardian is meant to read as published content
/// (the assignment's own <see cref="Homework.InstructionsEn"/>/<see cref="Homework.InstructionsDv"/>
/// already are).
/// </summary>
public sealed class HomeworkSubmission
{
    public Guid Id { get; init; }

    public Guid HomeworkId { get; init; }

    public Guid StudentPersonId { get; init; }

    public HomeworkSubmissionStatus Status { get; set; }

    public string? SubmissionText { get; set; }

    public string? FileReference { get; set; }

    public DateTimeOffset SubmittedAtUtc { get; set; }

    public int? Score { get; set; }

    public string? FeedbackText { get; set; }

    public Guid? GradedByPersonId { get; set; }

    public DateTimeOffset? GradedAtUtc { get; set; }
}
