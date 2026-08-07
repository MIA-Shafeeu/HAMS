namespace HAMS.Platform.Access;

/// <summary>
/// Everything needed to upsert one scoped <c>AccessGrant</c> row (build plan §4) from a source
/// module's assignment/relationship table. Dimensions left null are wildcards, exactly like on
/// <c>AccessGrant</c> itself.
/// </summary>
public sealed record ScopedAccessGrant(
    Guid PersonId,
    Guid RoleId,
    Guid? SchoolId,
    Guid? CampusId,
    Guid? AcademicYearId,
    Guid? KeyStageId,
    Guid? GradeId,
    Guid? ClassId,
    Guid? SubjectId,
    Guid? StudentId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string SourceType,
    Guid SourceId);
