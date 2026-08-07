using HAMS.Platform.Common.Contracts;

namespace HAMS.OrgCurriculum.Domain;

/// <summary>Configurable lookup (build plan §1.6, per explicit user instruction that holiday types must be configurable), not an enum.</summary>
public sealed class HolidayType : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class HolidayTypeCodes
{
    public const string PublicHoliday = "PUBLIC_HOLIDAY";
    public const string ReligiousHoliday = "RELIGIOUS_HOLIDAY";
    public const string SchoolDeclared = "SCHOOL_DECLARED";
}
