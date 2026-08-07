using HAMS.LearningDelivery.Domain;

namespace HAMS.LearningDelivery.Infrastructure;

/// <summary>Fixed reference data seeded via EF Core migrations — see <c>AccessSeedData</c> for the same pattern.</summary>
internal static class LearningDeliverySeedData
{
    public static readonly ResourceType[] ResourceTypes =
    [
        new() { Id = new Guid("00000000-0000-0000-0016-000000000001"), Code = ResourceTypeCodes.Document, Name = "Document", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0016-000000000002"), Code = ResourceTypeCodes.Video, Name = "Video", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0016-000000000003"), Code = ResourceTypeCodes.Link, Name = "Link", DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0016-000000000004"), Code = ResourceTypeCodes.Other, Name = "Other", DisplayOrder = 4 },
    ];

    public static readonly EvidenceType[] EvidenceTypes =
    [
        new() { Id = new Guid("00000000-0000-0000-0018-000000000001"), Code = EvidenceTypeCodes.Observation, Name = "Observation", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0018-000000000002"), Code = EvidenceTypeCodes.WorkSample, Name = "Work Sample", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0018-000000000003"), Code = EvidenceTypeCodes.Quiz, Name = "Quiz", DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0018-000000000004"), Code = EvidenceTypeCodes.AnecdotalNote, Name = "Anecdotal Note", DisplayOrder = 4 },
        new() { Id = new Guid("00000000-0000-0000-0018-000000000005"), Code = EvidenceTypeCodes.RatingScale, Name = "Rating Scale", DisplayOrder = 5 },
        new() { Id = new Guid("00000000-0000-0000-0018-000000000006"), Code = EvidenceTypeCodes.Checklist, Name = "Checklist", DisplayOrder = 6 },
        new() { Id = new Guid("00000000-0000-0000-0018-000000000007"), Code = EvidenceTypeCodes.PortfolioReference, Name = "Portfolio Reference", DisplayOrder = 7 },
        new() { Id = new Guid("00000000-0000-0000-0018-000000000008"), Code = EvidenceTypeCodes.Other, Name = "Other", DisplayOrder = 8 },
    ];

    // NameDv left null deliberately — see KeyCompetency's doc comment.
    public static readonly KeyCompetency[] KeyCompetencies =
    [
        new() { Id = new Guid("00000000-0000-0000-0019-000000000001"), Code = KeyCompetencyCodes.PractisingIslam, NameEn = "Practising Islam", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0019-000000000002"), Code = KeyCompetencyCodes.UnderstandingManagingSelf, NameEn = "Understanding & Managing Self", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0019-000000000003"), Code = KeyCompetencyCodes.ThinkingCriticallyCreatively, NameEn = "Thinking Critically & Creatively", DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0019-000000000004"), Code = KeyCompetencyCodes.RelatingToPeople, NameEn = "Relating to People", DisplayOrder = 4 },
        new() { Id = new Guid("00000000-0000-0000-0019-000000000005"), Code = KeyCompetencyCodes.MakingMeaning, NameEn = "Making Meaning", DisplayOrder = 5 },
        new() { Id = new Guid("00000000-0000-0000-0019-000000000006"), Code = KeyCompetencyCodes.LivingHealthyLife, NameEn = "Living a Healthy Life", DisplayOrder = 6 },
        new() { Id = new Guid("00000000-0000-0000-0019-000000000007"), Code = KeyCompetencyCodes.UsingSustainablePractices, NameEn = "Using Sustainable Practices", DisplayOrder = 7 },
        new() { Id = new Guid("00000000-0000-0000-0019-000000000008"), Code = KeyCompetencyCodes.UsingTechnologyMedia, NameEn = "Using Technology & the Media", DisplayOrder = 8 },
    ];
}
