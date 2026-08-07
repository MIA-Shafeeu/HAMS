using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Application;

internal sealed class HomeworkService(LearningDeliveryDbContext dbContext, IClock clock) : IHomeworkService
{
    public async Task<Guid> CreateAsync(
        Guid classId, Guid subjectId, Guid? teachingTopicId, string titleEn, string titleDv,
        string instructionsEn, string instructionsDv, DateOnly assignedDate, DateOnly dueDate,
        int? maxScore, Guid assignedByPersonId, CancellationToken cancellationToken = default)
    {
        if (dueDate < assignedDate)
        {
            throw new InvalidOperationException("Due date cannot be before the assigned date.");
        }

        var homework = new Homework
        {
            Id = Guid.NewGuid(), ClassId = classId, SubjectId = subjectId, TeachingTopicId = teachingTopicId,
            TitleEn = titleEn, TitleDv = titleDv, InstructionsEn = instructionsEn, InstructionsDv = instructionsDv,
            AssignedDate = assignedDate, DueDate = dueDate, MaxScore = maxScore, AssignedByPersonId = assignedByPersonId,
            CreatedAtUtc = clock.UtcNow,
        };
        dbContext.Homeworks.Add(homework);
        await dbContext.SaveChangesAsync(cancellationToken);

        return homework.Id;
    }

    public Task<Homework?> GetAsync(Guid homeworkId, CancellationToken cancellationToken = default) =>
        dbContext.Homeworks.SingleOrDefaultAsync(h => h.Id == homeworkId, cancellationToken);

    public async Task<IReadOnlyList<Homework>> ListForClassAsync(Guid classId, CancellationToken cancellationToken = default) =>
        await dbContext.Homeworks.Where(h => h.ClassId == classId).OrderByDescending(h => h.DueDate).ToListAsync(cancellationToken);
}
