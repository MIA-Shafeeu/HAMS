namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// A teaching resource attached to a <see cref="TeachingTopic"/>. Deliberately a simple
/// file/URL reference rather than routing through <c>Platform.Documents</c>' full upload/
/// versioning/checksum/malware-scan pipeline — that kernel remains a stub until a phase that
/// actually needs on-prem file storage with virus scanning builds it out; a resource here is
/// commonly just an external link or a path to a file already on a shared drive.
/// </summary>
public sealed class Resource
{
    public Guid Id { get; init; }

    public Guid TeachingTopicId { get; init; }

    public required string TitleEn { get; set; }

    public required string TitleDv { get; set; }

    public Guid ResourceTypeId { get; set; }

    public required string FileReference { get; set; }

    public Guid UploadedByPersonId { get; init; }
}
