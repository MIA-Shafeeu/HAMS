using HAMS.Platform.Access.Domain;

namespace HAMS.Platform.Access.Infrastructure;

/// <summary>
/// Reference data seeded via EF Core migrations (<c>HasData</c>) — fixed, well-known ids so the
/// seed is idempotent across every environment. Schools may still add further <see cref="Role"/>/
/// <see cref="ConfidentialityTier"/> rows at runtime (build plan §1.6); this is the starting set
/// from the SRS's IAM/§5 model, not an exhaustive enum.
/// </summary>
internal static class AccessSeedData
{
    public static readonly Role[] Roles =
    [
        new() { Id = new Guid("00000000-0000-0000-0001-000000000001"), Code = RoleCodes.SystemAdministrator, Name = "System Administrator", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0001-000000000002"), Code = RoleCodes.SchoolAdministrator, Name = "School Administrator", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0001-000000000003"), Code = RoleCodes.Principal, Name = "Principal", DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0001-000000000004"), Code = RoleCodes.DeputyPrincipal, Name = "Deputy Principal", DisplayOrder = 4 },
        new() { Id = new Guid("00000000-0000-0000-0001-000000000005"), Code = RoleCodes.ClassTeacher, Name = "Class Teacher", DisplayOrder = 5 },
        new() { Id = new Guid("00000000-0000-0000-0001-000000000006"), Code = RoleCodes.SubjectTeacher, Name = "Subject Teacher", DisplayOrder = 6 },
        new() { Id = new Guid("00000000-0000-0000-0001-000000000007"), Code = RoleCodes.LeadingTeacher, Name = "Leading Teacher", DisplayOrder = 7 },
        new() { Id = new Guid("00000000-0000-0000-0001-000000000008"), Code = RoleCodes.Student, Name = "Student", DisplayOrder = 8 },
        new() { Id = new Guid("00000000-0000-0000-0001-000000000009"), Code = RoleCodes.Guardian, Name = "Guardian", DisplayOrder = 9 },
        new() { Id = new Guid("00000000-0000-0000-0001-000000000010"), Code = RoleCodes.RegulatoryOfficer, Name = "Regulatory Officer", DisplayOrder = 10 },
        new() { Id = new Guid("00000000-0000-0000-0001-000000000011"), Code = RoleCodes.SchoolInspector, Name = "School Inspector", DisplayOrder = 11 },
        new() { Id = new Guid("00000000-0000-0000-0001-000000000012"), Code = RoleCodes.Auditor, Name = "Auditor", DisplayOrder = 12 },
    ];

    public static readonly ConfidentialityTier[] ConfidentialityTiers =
    [
        new() { Id = new Guid("00000000-0000-0000-0002-000000000001"), Code = ConfidentialityTierCodes.Restricted, Name = "Restricted", Rank = 1, DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0002-000000000002"), Code = ConfidentialityTierCodes.Safeguarding, Name = "Safeguarding", Rank = 2, DisplayOrder = 2 },
    ];
}
