using HAMS.PeopleEnrollment.Domain;

namespace HAMS.PeopleEnrollment.Infrastructure;

/// <summary>
/// Fixed reference data seeded via EF Core migrations — see <c>AccessSeedData</c>/<c>OrgSeedData</c>
/// for the same pattern. <see cref="Atolls"/> covers the real 20 Maldivian administrative atolls
/// (codes and English administrative names are well-established public data); <see cref="Atoll.NameDv"/>
/// is deliberately left null here rather than guessed — Thaana spelling of full atoll names should
/// be entered/verified by someone fluent in Dhivehi, not asserted by seed data. <see cref="Islands"/>
/// seeds only the school's own island; schools add the rest as real addresses need them.
/// </summary>
internal static class PeopleSeedData
{
    public static readonly Guid ThaaAtollId = new("00000000-0000-0000-0008-000000000015");

    public static readonly Atoll[] Atolls =
    [
        new() { Id = new Guid("00000000-0000-0000-0008-000000000001"), Code = "HA", NameEn = "Haa Alifu", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000002"), Code = "HDh", NameEn = "Haa Dhaalu", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000003"), Code = "Sh", NameEn = "Shaviyani", DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000004"), Code = "N", NameEn = "Noonu", DisplayOrder = 4 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000005"), Code = "R", NameEn = "Raa", DisplayOrder = 5 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000006"), Code = "B", NameEn = "Baa", DisplayOrder = 6 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000007"), Code = "Lh", NameEn = "Lhaviyani", DisplayOrder = 7 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000008"), Code = "K", NameEn = "Kaafu", DisplayOrder = 8 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000009"), Code = "AA", NameEn = "Alifu Alifu", DisplayOrder = 9 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000010"), Code = "ADh", NameEn = "Alifu Dhaalu", DisplayOrder = 10 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000011"), Code = "V", NameEn = "Vaavu", DisplayOrder = 11 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000012"), Code = "M", NameEn = "Meemu", DisplayOrder = 12 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000013"), Code = "F", NameEn = "Faafu", DisplayOrder = 13 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000014"), Code = "Dh", NameEn = "Dhaalu", DisplayOrder = 14 },
        new() { Id = ThaaAtollId, Code = "Th", NameEn = "Thaa", DisplayOrder = 15 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000016"), Code = "L", NameEn = "Laamu", DisplayOrder = 16 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000017"), Code = "GA", NameEn = "Gaafu Alifu", DisplayOrder = 17 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000018"), Code = "GDh", NameEn = "Gaafu Dhaalu", DisplayOrder = 18 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000019"), Code = "Gn", NameEn = "Gnaviyani", DisplayOrder = 19 },
        new() { Id = new Guid("00000000-0000-0000-0008-000000000020"), Code = "S", NameEn = "Seenu", DisplayOrder = 20 },
    ];

    public static readonly Island[] Islands =
    [
        new() { Id = new Guid("00000000-0000-0000-0009-000000000001"), AtollId = ThaaAtollId, Code = "HIRILANDHOO", NameEn = "Hirilandhoo", DisplayOrder = 1 },
    ];

    public static readonly EmploymentStatus[] EmploymentStatuses =
    [
        new() { Id = new Guid("00000000-0000-0000-0010-000000000001"), Code = EmploymentStatusCodes.Active, Name = "Active", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0010-000000000002"), Code = EmploymentStatusCodes.OnLeave, Name = "On Leave", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0010-000000000003"), Code = EmploymentStatusCodes.Resigned, Name = "Resigned", DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0010-000000000004"), Code = EmploymentStatusCodes.Retired, Name = "Retired", DisplayOrder = 4 },
    ];

    public static readonly RelationshipType[] RelationshipTypes =
    [
        new() { Id = new Guid("00000000-0000-0000-0011-000000000001"), Code = RelationshipTypeCodes.Mother, Name = "Mother", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0011-000000000002"), Code = RelationshipTypeCodes.Father, Name = "Father", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0011-000000000003"), Code = RelationshipTypeCodes.Grandparent, Name = "Grandparent", DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0011-000000000004"), Code = RelationshipTypeCodes.LegalGuardian, Name = "Legal Guardian", DisplayOrder = 4 },
        new() { Id = new Guid("00000000-0000-0000-0011-000000000005"), Code = RelationshipTypeCodes.Other, Name = "Other", DisplayOrder = 5 },
    ];

    public static readonly EnrollmentType[] EnrollmentTypes =
    [
        new() { Id = new Guid("00000000-0000-0000-0012-000000000001"), Code = EnrollmentTypeCodes.Ordinary, Name = "Ordinary", DisplayOrder = 1 },
    ];
}
