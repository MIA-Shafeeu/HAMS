namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// A subject+grade+year teaching plan (build plan §3 evaluation chain: <c>SchemeOfWork →
/// SchemeOfWorkItem → TeachingTopic → LessonPlan → LessonSession</c>). <see cref="SubjectId"/>/
/// <see cref="GradeId"/> are loose references into OrgCurriculum's "org" schema.
/// </summary>
public sealed class SchemeOfWork
{
    public Guid Id { get; init; }

    public Guid SubjectId { get; init; }

    public Guid GradeId { get; init; }

    public Guid AcademicYearId { get; init; }

    public required string Title { get; set; }
}
