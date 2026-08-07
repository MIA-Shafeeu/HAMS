using HAMS.Platform.Common.Contracts;

namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// Configurable lookup (build plan §1.6), not an enum. Dhivehi is used school-wide in Foundation
/// Stage and English becomes the default from Primary onward, but this is overridden per-subject
/// on <see cref="Subject.DefaultMediumOfInstructionId"/> — Quran/Islam/Dhivehi-language subjects
/// keep their own language regardless of the school's overall medium (build plan §3/§7).
/// </summary>
public sealed class MediumOfInstruction : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class MediumOfInstructionCodes
{
    public const string Dhivehi = "DHIVEHI";
    public const string English = "ENGLISH";
}
