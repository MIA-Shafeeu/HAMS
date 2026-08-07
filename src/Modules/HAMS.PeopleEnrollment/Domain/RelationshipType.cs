using HAMS.Platform.Common.Contracts;

namespace HAMS.PeopleEnrollment.Domain;

/// <summary>Configurable lookup (build plan §1.6 explicitly names "guardian RelationshipType"), not an enum.</summary>
public sealed class RelationshipType : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class RelationshipTypeCodes
{
    public const string Mother = "MOTHER";
    public const string Father = "FATHER";
    public const string Grandparent = "GRANDPARENT";
    public const string LegalGuardian = "LEGAL_GUARDIAN";
    public const string Other = "OTHER";
}
