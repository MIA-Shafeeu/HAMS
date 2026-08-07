using HAMS.Platform.Common.Contracts;

namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// How a <see cref="Subject"/> is delivered — configurable lookup, not an enum (build plan §1.6),
/// even though only two rows exist today. Exists specifically because ICT is cross-curricular/
/// integrated rather than a standalone timetabled subject below Key Stage 4 per the real National
/// Curriculum Framework (build plan §3): an <c>Integrated</c> subject consumes no timetable
/// periods of its own.
/// </summary>
public sealed class DeliveryMode : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class DeliveryModeCodes
{
    public const string Timetabled = "TIMETABLED";
    public const string Integrated = "INTEGRATED";
}
