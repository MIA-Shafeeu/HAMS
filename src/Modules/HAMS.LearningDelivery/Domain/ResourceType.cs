using HAMS.Platform.Common.Contracts;

namespace HAMS.LearningDelivery.Domain;

/// <summary>Configurable lookup (build plan §1.6), not an enum.</summary>
public sealed class ResourceType : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class ResourceTypeCodes
{
    public const string Document = "DOCUMENT";
    public const string Video = "VIDEO";
    public const string Link = "LINK";
    public const string Other = "OTHER";
}
