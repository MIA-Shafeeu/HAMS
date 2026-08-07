namespace HAMS.WebHost.Client.Portal;

public sealed record StudentHomeworkDto(Guid Id, Guid SubjectId, string TitleEn, string InstructionsEn, DateOnly AssignedDate, DateOnly DueDate, int? MaxScore);

public sealed record SubmitHomeworkDto(string? SubmissionText, string? FileReference);
