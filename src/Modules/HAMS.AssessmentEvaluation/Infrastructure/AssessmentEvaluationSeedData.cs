using HAMS.AssessmentEvaluation.Domain;

namespace HAMS.AssessmentEvaluation.Infrastructure;

/// <summary>Fixed reference data seeded via EF Core migrations — see <c>AccessSeedData</c> for the same pattern.</summary>
internal static class AssessmentEvaluationSeedData
{
    public static readonly AssessmentCategory[] AssessmentCategories =
    [
        new() { Id = new Guid("00000000-0000-0000-0020-000000000001"), Code = AssessmentCategoryCodes.TermExam, Name = "Term Exam", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0020-000000000002"), Code = AssessmentCategoryCodes.ContinuousAssessment, Name = "Continuous Assessment", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0020-000000000003"), Code = AssessmentCategoryCodes.Quiz, Name = "Quiz", DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0020-000000000004"), Code = AssessmentCategoryCodes.Project, Name = "Project", DisplayOrder = 4 },
        new() { Id = new Guid("00000000-0000-0000-0020-000000000005"), Code = AssessmentCategoryCodes.Other, Name = "Other", DisplayOrder = 5 },
    ];

    public static readonly ExternalExaminationBoard[] ExternalExaminationBoards =
    [
        new() { Id = new Guid("00000000-0000-0000-0021-000000000001"), Code = ExternalExaminationBoardCodes.Cambridge, Name = "Cambridge Assessment International Education", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0021-000000000002"), Code = ExternalExaminationBoardCodes.Edexcel, Name = "Pearson Edexcel", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0021-000000000003"), Code = ExternalExaminationBoardCodes.Ssc, Name = "Secondary School Certificate (SSC)", DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0021-000000000004"), Code = ExternalExaminationBoardCodes.Hsc, Name = "Higher Secondary Certificate (HSC)", DisplayOrder = 4 },
    ];

    public static readonly SpecialResultState[] SpecialResultStates =
    [
        new() { Id = new Guid("00000000-0000-0000-0022-000000000001"), Code = SpecialResultStateCodes.MedicalCertificateExcused, Name = "Medical Certificate Excused", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0022-000000000002"), Code = SpecialResultStateCodes.AuthorizedTravelMakeUp, Name = "Authorized Travel - Make-Up Exam", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0022-000000000003"), Code = SpecialResultStateCodes.CalibrationOnly, Name = "Calibration Only", DisplayOrder = 3 },
    ];

    public static readonly ResultAggregationRule[] ResultAggregationRules =
    [
        new() { Id = new Guid("00000000-0000-0000-0023-000000000001"), Code = ResultAggregationRuleCodes.Latest, Name = "Latest Attempt", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0023-000000000002"), Code = ResultAggregationRuleCodes.Highest, Name = "Highest Attempt", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0023-000000000003"), Code = ResultAggregationRuleCodes.Average, Name = "Attempt Average", DisplayOrder = 3 },
    ];

    // Not a real Ministry-published promotion rule (no such document was found the way the 2019
    // Assessment Policy circular grounded Phase 7's seed data) — a reasonable, clearly-labelled
    // starting default a school can rename/adjust via the existing admin endpoint, not a policy to
    // treat as authoritative.
    public static readonly PromotionPolicy[] PromotionPolicies =
    [
        new() { Id = new Guid("00000000-0000-0000-0025-000000000001"), Code = "STANDARD", Name = "Standard Promotion Policy", MinimumRank = 2, MinimumSubjectsRequiredToClear = 1 },
    ];
}
