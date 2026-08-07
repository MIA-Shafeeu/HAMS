namespace HAMS.OrgCurriculum.Application;

public sealed record CurriculumCsvImportResult(int StrandsCreated, int SubStrandsCreated, int OutcomesCreated, int IndicatorsCreated);

/// <summary>
/// Simple CSV bulk-entry for syllabus content (build plan Phase 2 scope note: "simple CSV import,"
/// bulk-spreadsheet-import polish is explicitly deferred). One row per <c>Indicator</c>; ancestor
/// Strand/SubStrand/LearningOutcome rows are created the first time their code is seen and reused
/// for every subsequent row sharing that code, so one CSV can describe a whole tree in flattened
/// form. Expected header: <c>StrandCode,StrandName,SubStrandCode,SubStrandName,OutcomeCode,
/// OutcomeDescription,IndicatorCode,IndicatorDescription</c>.
/// </summary>
public interface ICurriculumCsvImportService
{
    /// <summary>The target syllabus must be Draft — importing into a Published/Locked syllabus would violate the "old tree is frozen forever" rule (build plan §3).</summary>
    Task<CurriculumCsvImportResult> ImportAsync(Guid syllabusId, Stream csvStream, CancellationToken cancellationToken = default);
}
