using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Tests;

public class HomeworkSubmissionServiceTests
{
    private static LearningDeliveryDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LearningDeliveryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<Guid> SeedHomeworkAsync(LearningDeliveryDbContext db, DateOnly dueDate)
    {
        var homework = new Homework
        {
            Id = Guid.NewGuid(), ClassId = Guid.NewGuid(), SubjectId = Guid.NewGuid(),
            TitleEn = "Test", TitleDv = "Test", InstructionsEn = "x", InstructionsDv = "x",
            AssignedDate = dueDate.AddDays(-5), DueDate = dueDate, AssignedByPersonId = Guid.NewGuid(), CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Homeworks.Add(homework);
        await db.SaveChangesAsync();
        return homework.Id;
    }

    private static HomeworkSubmissionService CreateService(LearningDeliveryDbContext db, DateOnly? today = null) =>
        new(db, new FakeClock(today ?? new DateOnly(2026, 8, 5)));

    [Fact]
    public async Task SubmitAsync_on_or_before_the_due_date_is_marked_Submitted()
    {
        await using var db = CreateContext();
        var homeworkId = await SeedHomeworkAsync(db, dueDate: new DateOnly(2026, 8, 10));
        var service = CreateService(db, today: new DateOnly(2026, 8, 10));
        var studentId = Guid.NewGuid();

        await service.SubmitAsync(homeworkId, studentId, "My answer", null);

        var submission = await db.HomeworkSubmissions.SingleAsync(s => s.HomeworkId == homeworkId && s.StudentPersonId == studentId);
        Assert.Equal(HomeworkSubmissionStatus.Submitted, submission.Status);
    }

    [Fact]
    public async Task SubmitAsync_after_the_due_date_is_marked_Late()
    {
        await using var db = CreateContext();
        var homeworkId = await SeedHomeworkAsync(db, dueDate: new DateOnly(2026, 8, 10));
        var service = CreateService(db, today: new DateOnly(2026, 8, 11));
        var studentId = Guid.NewGuid();

        await service.SubmitAsync(homeworkId, studentId, "Late answer", null);

        var submission = await db.HomeworkSubmissions.SingleAsync(s => s.HomeworkId == homeworkId && s.StudentPersonId == studentId);
        Assert.Equal(HomeworkSubmissionStatus.Late, submission.Status);
    }

    [Fact]
    public async Task SubmitAsync_resubmitting_before_grading_updates_the_same_row_not_a_second_one()
    {
        await using var db = CreateContext();
        var homeworkId = await SeedHomeworkAsync(db, dueDate: new DateOnly(2026, 8, 10));
        var service = CreateService(db, today: new DateOnly(2026, 8, 5));
        var studentId = Guid.NewGuid();

        var firstId = await service.SubmitAsync(homeworkId, studentId, "Draft answer", null);
        var secondId = await service.SubmitAsync(homeworkId, studentId, "Final answer", null);

        Assert.Equal(firstId, secondId);
        var submissions = await db.HomeworkSubmissions.Where(s => s.HomeworkId == homeworkId).ToListAsync();
        Assert.Single(submissions);
        Assert.Equal("Final answer", submissions[0].SubmissionText);
    }

    [Fact]
    public async Task SubmitAsync_rejects_resubmission_once_already_graded()
    {
        await using var db = CreateContext();
        var homeworkId = await SeedHomeworkAsync(db, dueDate: new DateOnly(2026, 8, 10));
        var service = CreateService(db, today: new DateOnly(2026, 8, 5));
        var studentId = Guid.NewGuid();
        var submissionId = await service.SubmitAsync(homeworkId, studentId, "Answer", null);
        await service.GradeAsync(submissionId, 18, "Good work", Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(homeworkId, studentId, "New answer", null));
    }

    [Fact]
    public async Task GradeAsync_records_the_score_feedback_and_grader_and_flips_status_to_Graded()
    {
        await using var db = CreateContext();
        var homeworkId = await SeedHomeworkAsync(db, dueDate: new DateOnly(2026, 8, 10));
        var service = CreateService(db, today: new DateOnly(2026, 8, 5));
        var graderId = Guid.NewGuid();
        var submissionId = await service.SubmitAsync(homeworkId, Guid.NewGuid(), "Answer", null);

        await service.GradeAsync(submissionId, 15, "Well done", graderId);

        var submission = await db.HomeworkSubmissions.SingleAsync(s => s.Id == submissionId);
        Assert.Equal(HomeworkSubmissionStatus.Graded, submission.Status);
        Assert.Equal(15, submission.Score);
        Assert.Equal("Well done", submission.FeedbackText);
        Assert.Equal(graderId, submission.GradedByPersonId);
        Assert.NotNull(submission.GradedAtUtc);
    }

    [Fact]
    public async Task GradeAsync_throws_for_an_unknown_submission()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GradeAsync(Guid.NewGuid(), 10, null, Guid.NewGuid()));
    }
}
