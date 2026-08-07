namespace HAMS.WebHost.Client.Portal;

/// <summary>
/// Minimal client-side read shapes for the guardian/student portal (Phase C2/C3) — each captures only
/// the fields these pages render, not the full server-side domain entity. Kept local to the WASM
/// client rather than in HAMS.SharedContracts: these are portal-display projections, not a contract a
/// server module authors against (unlike the C1 auth DTOs, which mirror an actual request/response
/// wire shape both sides must agree on byte-for-byte).
/// </summary>
public sealed record SchoolRef(Guid Id, string Name);

public sealed record SubjectRef(Guid Id, string Name);

public sealed record AcademicYearRef(Guid Id, string Name);

public sealed record GuardianStudentSummaryDto(
    Guid StudentPersonId, bool CanViewAcademicRecords, bool CanViewAttendance, bool CanViewInterventionUpdates,
    bool CanViewBehaviourRecords, string NameEn, string NameDv, string AdmissionNumber);

public sealed record KeyStageEvaluationDto(Guid Id, Guid SubjectId, decimal? OverallPercentage, DateTimeOffset RecordedAtUtc);

public sealed record AttendanceRecordSummaryDto(DateOnly Date, string AttendanceStatusCode, string? Notes);

public sealed record InterventionUpdateSummaryDto(Guid SubjectId, DateOnly OpenedDate, bool IsOpen, DateOnly? ClosedDate);

public sealed record BehaviourIncidentSummaryDto(string CategoryName, bool IsPositive, DateOnly OccurredDate);

public sealed record HomeworkDto(Guid Id, Guid SubjectId, string TitleEn, DateOnly AssignedDate, DateOnly DueDate, int? MaxScore);

public sealed record ReportCardDto(Guid Id, Guid AcademicYearId, string NarrativeEn, string NextStepsEn, string ApprovalStatus, DateTimeOffset PreparedAtUtc);

public sealed record AcknowledgementStatusDto(bool Acknowledged, DateTimeOffset? AcknowledgedAtUtc);

public sealed record AcknowledgeRequestDto(string EntityType, string EntityId);
