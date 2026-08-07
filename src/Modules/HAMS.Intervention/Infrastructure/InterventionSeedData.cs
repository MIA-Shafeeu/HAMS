using HAMS.Intervention.Domain;

namespace HAMS.Intervention.Infrastructure;

/// <summary>Fixed reference data seeded via EF Core migrations — see <c>AccessSeedData</c> for the same pattern.</summary>
internal static class InterventionSeedData
{
    public static readonly InterventionType[] InterventionTypes =
    [
        new() { Id = new Guid("00000000-0000-0000-0024-000000000001"), Code = InterventionTypeCodes.AdditionalPractice, Name = "Additional Practice", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0024-000000000002"), Code = InterventionTypeCodes.OneOnOneSupport, Name = "One-on-One Support", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0024-000000000003"), Code = InterventionTypeCodes.PeerTutoring, Name = "Peer Tutoring", DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0024-000000000004"), Code = InterventionTypeCodes.ParentConference, Name = "Parent Conference", DisplayOrder = 4 },
        new() { Id = new Guid("00000000-0000-0000-0024-000000000005"), Code = InterventionTypeCodes.LearningSupportReferral, Name = "Learning Support Referral", DisplayOrder = 5 },
        new() { Id = new Guid("00000000-0000-0000-0024-000000000006"), Code = InterventionTypeCodes.Other, Name = "Other", DisplayOrder = 6 },
    ];

    public static readonly BehaviourCategory[] BehaviourCategories =
    [
        new() { Id = new Guid("00000000-0000-0000-0026-000000000001"), Code = BehaviourCategoryCodes.Merit, Name = "Merit", IsPositive = true, DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0026-000000000002"), Code = BehaviourCategoryCodes.Recognition, Name = "Recognition", IsPositive = true, DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0026-000000000003"), Code = BehaviourCategoryCodes.Disruption, Name = "Disruption", IsPositive = false, DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0026-000000000004"), Code = BehaviourCategoryCodes.Disrespect, Name = "Disrespect", IsPositive = false, DisplayOrder = 4 },
        new() { Id = new Guid("00000000-0000-0000-0026-000000000005"), Code = BehaviourCategoryCodes.Bullying, Name = "Bullying", IsPositive = false, DisplayOrder = 5 },
        new() { Id = new Guid("00000000-0000-0000-0026-000000000006"), Code = BehaviourCategoryCodes.Other, Name = "Other", IsPositive = false, DisplayOrder = 6 },
    ];
}
