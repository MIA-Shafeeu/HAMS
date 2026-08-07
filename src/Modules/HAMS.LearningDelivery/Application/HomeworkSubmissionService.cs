using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Application;

internal sealed class HomeworkSubmissionService(LearningDeliveryDbContext dbContext, IClock clock) : IHomeworkSubmissionService
{
    public async Task<Guid> SubmitAsync(
        Guid homeworkId, Guid studentPersonId, string? submissionText, string? fileReference, CancellationToken cancellationToken = default)
    {
        var homework = await dbContext.Homeworks.SingleOrDefaultAsync(h => h.Id == homeworkId, cancellationToken)
            ?? throw new InvalidOperationException("Homework not found.");

        var now = clock.UtcNow;
        var existing = await dbContext.HomeworkSubmissions
            .SingleOrDefaultAsync(s => s.HomeworkId == homeworkId && s.StudentPersonId == studentPersonId, cancellationToken);

        if (existing is not null && existing.Status == HomeworkSubmissionStatus.Graded)
        {
            throw new InvalidOperationException("This submission has already been graded and can no longer be resubmitted.");
        }

        var status = clock.TodayUtc > homework.DueDate ? HomeworkSubmissionStatus.Late : HomeworkSubmissionStatus.Submitted;

        if (existing is not null)
        {
            existing.SubmissionText = submissionText;
            existing.FileReference = fileReference;
            existing.SubmittedAtUtc = now;
            existing.Status = status;
            await dbContext.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var submission = new HomeworkSubmission
        {
            Id = Guid.NewGuid(), HomeworkId = homeworkId, StudentPersonId = studentPersonId,
            SubmissionText = submissionText, FileReference = fileReference, SubmittedAtUtc = now, Status = status,
        };
        dbContext.HomeworkSubmissions.Add(submission);
        await dbContext.SaveChangesAsync(cancellationToken);

        return submission.Id;
    }

    public async Task GradeAsync(Guid submissionId, int? score, string? feedbackText, Guid gradedByPersonId, CancellationToken cancellationToken = default)
    {
        var submission = await dbContext.HomeworkSubmissions.SingleOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new InvalidOperationException("Submission not found.");

        submission.Score = score;
        submission.FeedbackText = feedbackText;
        submission.GradedByPersonId = gradedByPersonId;
        submission.GradedAtUtc = clock.UtcNow;
        submission.Status = HomeworkSubmissionStatus.Graded;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HomeworkSubmission>> ListForHomeworkAsync(Guid homeworkId, CancellationToken cancellationToken = default) =>
        await dbContext.HomeworkSubmissions.Where(s => s.HomeworkId == homeworkId).ToListAsync(cancellationToken);

    public Task<HomeworkSubmission?> GetForStudentAsync(Guid homeworkId, Guid studentPersonId, CancellationToken cancellationToken = default) =>
        dbContext.HomeworkSubmissions.SingleOrDefaultAsync(s => s.HomeworkId == homeworkId && s.StudentPersonId == studentPersonId, cancellationToken);
}
