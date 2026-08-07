namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// A specific declared no-school date — public holidays (e.g. Independence Day), religious
/// holidays (e.g. Eid), or a school-declared closure (e.g. a weather closure), per
/// <see cref="HolidayType"/>. Bilingual name per the established convention (build plan, user
/// instruction): a holiday name is exactly the kind of official free text that appears on
/// printed calendars/report cards in both languages.
/// </summary>
public sealed class Holiday
{
    public Guid Id { get; init; }

    public Guid SchoolId { get; init; }

    public DateOnly Date { get; init; }

    public Guid HolidayTypeId { get; set; }

    public required string NameEn { get; set; }

    public required string NameDv { get; set; }
}
