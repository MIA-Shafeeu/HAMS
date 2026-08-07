using HAMS.Platform.Common.Contracts;

namespace HAMS.Intervention.Domain;

/// <summary>
/// Configurable lookup (build plan §1.6 explicitly names "InterventionType" as an example), not
/// an enum — what kind of support a school offers changes over time and varies by school.
/// </summary>
public sealed class InterventionType : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class InterventionTypeCodes
{
    public const string AdditionalPractice = "ADDITIONAL_PRACTICE";
    public const string OneOnOneSupport = "ONE_ON_ONE_SUPPORT";
    public const string PeerTutoring = "PEER_TUTORING";
    public const string ParentConference = "PARENT_CONFERENCE";
    public const string LearningSupportReferral = "LEARNING_SUPPORT_REFERRAL";
    public const string Other = "OTHER";
}
