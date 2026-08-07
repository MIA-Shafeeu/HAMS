using HAMS.TeachingTimetable.Domain;

namespace HAMS.TeachingTimetable.Infrastructure;

/// <summary>Fixed reference data seeded via EF Core migrations — see <c>AccessSeedData</c> for the same pattern.</summary>
internal static class TeachingSeedData
{
    public static readonly AssignmentRole[] AssignmentRoles =
    [
        new() { Id = new Guid("00000000-0000-0000-0013-000000000001"), Code = AssignmentRoleCodes.Ordinary, Name = "Ordinary", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0013-000000000002"), Code = AssignmentRoleCodes.Substitute, Name = "Substitute", DisplayOrder = 2 },
    ];
}
