namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// A teacher-created unit of teaching grouped under a <see cref="SchemeOfWorkItem"/> — teachers
/// organise their own delivery around the official outcome, but never edit the outcome itself
/// (BR-008); this table is where their own organisation lives instead. Bilingual name per the
/// established convention (a topic title is exactly the kind of "Name"-like field that benefits
/// from both languages, e.g. on a printed scheme of work).
/// </summary>
public sealed class TeachingTopic
{
    public Guid Id { get; init; }

    public Guid SchemeOfWorkItemId { get; init; }

    public required string NameEn { get; set; }

    public required string NameDv { get; set; }

    public int DisplayOrder { get; set; }
}
