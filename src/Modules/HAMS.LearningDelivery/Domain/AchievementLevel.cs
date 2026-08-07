namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// One level within an <see cref="AchievementScale"/> (e.g. "Working Towards" / "Working At" /
/// "Working Beyond") — configurable per school, not an enum. <see cref="Rank"/> is the stable
/// ordering used to compare levels (higher = more advanced) and to break ties in
/// <c>IRecommendedLevelEngine</c>'s mode calculation; renaming <see cref="Name"/> never disturbs
/// history since every historical <see cref="LearningEvidence"/>/<see cref="MasteryEvaluation"/>
/// row stores the level's <see cref="Id"/>, never a copy of its name.
///
/// Kept single-language like <c>AttendanceStatus</c>/<c>HolidayType</c>/<c>ResourceType</c>
/// (categorical config labels), not bilingual like <c>TeachingTopic</c>/<c>Holiday</c> (specific
/// named content) — a judgment call, not automatic; if a future report-card phase needs a Dhivehi
/// label for the level shown to guardians, add <c>NameDv</c> then rather than guessing now.
/// </summary>
public sealed class AchievementLevel
{
    public Guid Id { get; init; }

    public Guid AchievementScaleId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int Rank { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
