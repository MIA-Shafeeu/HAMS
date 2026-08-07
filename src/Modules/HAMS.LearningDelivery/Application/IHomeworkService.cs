using HAMS.LearningDelivery.Domain;

namespace HAMS.LearningDelivery.Application;

public interface IHomeworkService
{
    Task<Guid> CreateAsync(
        Guid classId, Guid subjectId, Guid? teachingTopicId, string titleEn, string titleDv,
        string instructionsEn, string instructionsDv, DateOnly assignedDate, DateOnly dueDate,
        int? maxScore, Guid assignedByPersonId, CancellationToken cancellationToken = default);

    Task<Homework?> GetAsync(Guid homeworkId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Homework>> ListForClassAsync(Guid classId, CancellationToken cancellationToken = default);
}
