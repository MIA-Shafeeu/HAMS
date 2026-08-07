using HAMS.Platform.Common.Contracts;

namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// Configurable lookup (build plan §1.6 explicitly names "special assessment result states" as an
/// example), not an enum — the Ministry's 2019 Assessment Policy names several real special
/// circumstances: a Medical Certificate submitted within the test week substitutes the
/// continuous-assessment outcome for a missed exam; Ministry/school-authorised travel to represent
/// the school/country gets a scheduled make-up exam; a student returning from studying abroad has
/// Islam/Dhivehi/Quran assessed for calibration only in their first year back. Null on
/// <see cref="AssessmentResult.SpecialResultStateId"/> means "ordinary" — no row needed for the
/// common case.
/// </summary>
public sealed class SpecialResultState : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class SpecialResultStateCodes
{
    public const string MedicalCertificateExcused = "MEDICAL_CERTIFICATE_EXCUSED";
    public const string AuthorizedTravelMakeUp = "AUTHORIZED_TRAVEL_MAKEUP";
    public const string CalibrationOnly = "CALIBRATION_ONLY";
}
