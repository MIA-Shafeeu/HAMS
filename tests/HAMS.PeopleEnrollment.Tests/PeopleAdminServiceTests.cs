using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Domain;
using HAMS.PeopleEnrollment.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.PeopleEnrollment.Tests;

public class PeopleAdminServiceTests
{
    private static PeopleDbContext CreateContext() => new(
        new DbContextOptionsBuilder<PeopleDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Address SampleAddress(Guid islandId) => new()
    {
        IslandId = islandId, RoadEn = "Coral Way", RoadDv = "ކޮރަލް ވޭ",
        HouseNameEn = "Asseyri", HouseNameDv = "އަސެއިރި",
    };

    [Fact]
    public async Task CreateIslandAsync_links_to_the_given_atoll()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var atollId = await service.CreateAtollAsync("TH", "Thaa", "ތ", 1);

        var islandId = await service.CreateIslandAsync(atollId, "HND", "Hirilandhoo", "ހިރިލަންދޫ", 1);

        var island = Assert.Single(await service.GetIslandsAsync(atollId));
        Assert.Equal(islandId, island.Id);
    }

    [Fact]
    public async Task CreatePersonAsync_persists_and_is_retrievable_by_id()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var atollId = await service.CreateAtollAsync("TH", "Thaa", null, 1);
        var islandId = await service.CreateIslandAsync(atollId, "HND", "Hirilandhoo", null, 1);

        var personId = await service.CreatePersonAsync("Ahmed Naseer", "އަހްމަދު ނަސީރު", new DateOnly(2012, 5, 14), SampleAddress(islandId), "7771234", null);

        var person = await service.GetPersonAsync(personId);
        Assert.NotNull(person);
        Assert.Equal("Ahmed Naseer", person!.NameEn);
    }

    [Fact]
    public async Task GetStudentProfilesAsync_joins_person_name_fields_and_orders_by_English_name()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var atollId = await service.CreateAtollAsync("TH", "Thaa", null, 1);
        var islandId = await service.CreateIslandAsync(atollId, "HND", "Hirilandhoo", null, 1);
        var beePersonId = await service.CreatePersonAsync("Beefan", "ބީފާން", new DateOnly(2013, 1, 1), SampleAddress(islandId), null, null);
        var aliPersonId = await service.CreatePersonAsync("Ali", "އަލީ", new DateOnly(2013, 1, 1), SampleAddress(islandId), null, null);
        await service.CreateStudentProfileAsync(beePersonId, "A002", new DateOnly(2020, 1, 1));
        await service.CreateStudentProfileAsync(aliPersonId, "A001", new DateOnly(2020, 1, 1));

        var students = await service.GetStudentProfilesAsync();

        Assert.Equal(["Ali", "Beefan"], students.Select(s => s.NameEn));
    }

    [Fact]
    public async Task CreateStaffProfileAsync_resolves_employment_status_by_code()
    {
        await using var db = CreateContext();
        var statusId = Guid.NewGuid();
        db.EmploymentStatuses.Add(new EmploymentStatus { Id = statusId, Code = EmploymentStatusCodes.Active, Name = "Active", IsActive = true });
        await db.SaveChangesAsync();
        var service = new PeopleAdminService(db);
        var atollId = await service.CreateAtollAsync("TH", "Thaa", null, 1);
        var islandId = await service.CreateIslandAsync(atollId, "HND", "Hirilandhoo", null, 1);
        var personId = await service.CreatePersonAsync("Fathimath Shifa", "ފާތިމަތު ޝިފާ", new DateOnly(1990, 1, 1), SampleAddress(islandId), null, null);

        var staffProfileId = await service.CreateStaffProfileAsync(personId, "EMP001", new DateOnly(2015, 1, 1), EmploymentStatusCodes.Active);

        var staff = Assert.Single(await service.GetStaffProfilesAsync());
        Assert.Equal(staffProfileId, staff.Id);
        Assert.Equal(personId, staff.PersonId);
        Assert.Equal(statusId, staff.EmploymentStatusId);
    }

    [Fact]
    public async Task CreateStaffProfileAsync_throws_for_an_unknown_employment_status_code()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var atollId = await service.CreateAtollAsync("TH", "Thaa", null, 1);
        var islandId = await service.CreateIslandAsync(atollId, "HND", "Hirilandhoo", null, 1);
        var personId = await service.CreatePersonAsync("X", "Y", new DateOnly(1990, 1, 1), SampleAddress(islandId), null, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateStaffProfileAsync(personId, "EMP001", new DateOnly(2015, 1, 1), "NONEXISTENT"));
    }

    [Fact]
    public async Task AddStaffQualificationAsync_is_retrievable_via_GetStaffQualificationsAsync()
    {
        await using var db = CreateContext();
        db.EmploymentStatuses.Add(new EmploymentStatus { Id = Guid.NewGuid(), Code = EmploymentStatusCodes.Active, Name = "Active", IsActive = true });
        await db.SaveChangesAsync();
        var service = new PeopleAdminService(db);
        var atollId = await service.CreateAtollAsync("TH", "Thaa", null, 1);
        var islandId = await service.CreateIslandAsync(atollId, "HND", "Hirilandhoo", null, 1);
        var personId = await service.CreatePersonAsync("X", "Y", new DateOnly(1990, 1, 1), SampleAddress(islandId), null, null);
        var staffProfileId = await service.CreateStaffProfileAsync(personId, "EMP001", new DateOnly(2015, 1, 1), EmploymentStatusCodes.Active);

        await service.AddStaffQualificationAsync(staffProfileId, "B.Ed Primary Education", "Maldives National University", 2014);

        var qualification = Assert.Single(await service.GetStaffQualificationsAsync(staffProfileId));
        Assert.Equal("B.Ed Primary Education", qualification.Title);
    }

    [Fact]
    public async Task GetGuardianProfilesAsync_joins_person_name_fields()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var atollId = await service.CreateAtollAsync("TH", "Thaa", null, 1);
        var islandId = await service.CreateIslandAsync(atollId, "HND", "Hirilandhoo", null, 1);
        var personId = await service.CreatePersonAsync("Hassan Waheed", "ހަސަން ވަހީދު", new DateOnly(1985, 1, 1), SampleAddress(islandId), null, null);

        await service.CreateGuardianProfileAsync(personId);

        var guardian = Assert.Single(await service.GetGuardianProfilesAsync());
        Assert.Equal("Hassan Waheed", guardian.NameEn);
    }

    [Fact]
    public async Task CreateRelationshipTypeAsync_persists_and_is_returned_by_GetRelationshipTypesAsync()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);

        var id = await service.CreateRelationshipTypeAsync("STEPPARENT", "Stepparent", 1);

        var relationshipType = Assert.Single(await service.GetRelationshipTypesAsync());
        Assert.Equal(id, relationshipType.Id);
        Assert.Equal("STEPPARENT", relationshipType.Code);
        Assert.Equal("Stepparent", relationshipType.Name);
    }

    [Fact]
    public async Task SetRelationshipTypeActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var id = await service.CreateRelationshipTypeAsync("STEPPARENT", "Stepparent", 1);

        await service.SetRelationshipTypeActiveAsync(id, false);

        Assert.Empty(await service.GetRelationshipTypesAsync());
        var relationshipType = await db.RelationshipTypes.FindAsync(id);
        Assert.False(relationshipType!.IsActive);
    }

    [Fact]
    public async Task SetRelationshipTypeActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetRelationshipTypeActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task GetAllRelationshipTypesAsync_includes_inactive_types_unlike_GetRelationshipTypesAsync()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var id = await service.CreateRelationshipTypeAsync("STEPPARENT", "Stepparent", 1);
        await service.SetRelationshipTypeActiveAsync(id, false);

        Assert.Contains(await service.GetAllRelationshipTypesAsync(), t => t.Id == id);
    }

    [Fact]
    public async Task CreateRestrictionTypeAsync_persists_and_is_returned_by_GetRestrictionTypesAsync()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);

        var id = await service.CreateRestrictionTypeAsync("COURT_ORDER", "Court Order", 1);

        var restrictionType = Assert.Single(await service.GetRestrictionTypesAsync());
        Assert.Equal(id, restrictionType.Id);
        Assert.Equal("COURT_ORDER", restrictionType.Code);
        Assert.Equal("Court Order", restrictionType.Name);
    }

    [Fact]
    public async Task SetRestrictionTypeActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var id = await service.CreateRestrictionTypeAsync("COURT_ORDER", "Court Order", 1);

        await service.SetRestrictionTypeActiveAsync(id, false);

        Assert.Empty(await service.GetRestrictionTypesAsync());
        var restrictionType = await db.RestrictionTypes.FindAsync(id);
        Assert.False(restrictionType!.IsActive);
    }

    [Fact]
    public async Task SetRestrictionTypeActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetRestrictionTypeActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task GetAllRestrictionTypesAsync_includes_inactive_types_unlike_GetRestrictionTypesAsync()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var id = await service.CreateRestrictionTypeAsync("COURT_ORDER", "Court Order", 1);
        await service.SetRestrictionTypeActiveAsync(id, false);

        Assert.Contains(await service.GetAllRestrictionTypesAsync(), t => t.Id == id);
    }

    [Fact]
    public async Task CreateEmploymentStatusAsync_persists_and_is_returned_by_GetEmploymentStatusesAsync()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);

        var id = await service.CreateEmploymentStatusAsync("CONTRACT", "Contract", 1);

        var status = Assert.Single(await service.GetEmploymentStatusesAsync());
        Assert.Equal(id, status.Id);
        Assert.Equal("CONTRACT", status.Code);
        Assert.Equal("Contract", status.Name);
    }

    [Fact]
    public async Task SetEmploymentStatusActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var id = await service.CreateEmploymentStatusAsync("CONTRACT", "Contract", 1);

        await service.SetEmploymentStatusActiveAsync(id, false);

        Assert.Empty(await service.GetEmploymentStatusesAsync());
        var status = await db.EmploymentStatuses.FindAsync(id);
        Assert.False(status!.IsActive);
    }

    [Fact]
    public async Task SetEmploymentStatusActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetEmploymentStatusActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task GetAllEmploymentStatusesAsync_includes_inactive_statuses_unlike_GetEmploymentStatusesAsync()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var id = await service.CreateEmploymentStatusAsync("CONTRACT", "Contract", 1);
        await service.SetEmploymentStatusActiveAsync(id, false);

        Assert.Contains(await service.GetAllEmploymentStatusesAsync(), s => s.Id == id);
    }

    [Fact]
    public async Task CreateEnrollmentTypeAsync_persists_and_is_returned_by_GetEnrollmentTypesAsync()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);

        var id = await service.CreateEnrollmentTypeAsync("REPEATING", "Repeating", 1);

        var enrollmentType = Assert.Single(await service.GetEnrollmentTypesAsync());
        Assert.Equal(id, enrollmentType.Id);
        Assert.Equal("REPEATING", enrollmentType.Code);
        Assert.Equal("Repeating", enrollmentType.Name);
    }

    [Fact]
    public async Task SetEnrollmentTypeActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var id = await service.CreateEnrollmentTypeAsync("REPEATING", "Repeating", 1);

        await service.SetEnrollmentTypeActiveAsync(id, false);

        Assert.Empty(await service.GetEnrollmentTypesAsync());
        var enrollmentType = await db.EnrollmentTypes.FindAsync(id);
        Assert.False(enrollmentType!.IsActive);
    }

    [Fact]
    public async Task SetEnrollmentTypeActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetEnrollmentTypeActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task GetAllEnrollmentTypesAsync_includes_inactive_types_unlike_GetEnrollmentTypesAsync()
    {
        await using var db = CreateContext();
        var service = new PeopleAdminService(db);
        var id = await service.CreateEnrollmentTypeAsync("REPEATING", "Repeating", 1);
        await service.SetEnrollmentTypeActiveAsync(id, false);

        Assert.Contains(await service.GetAllEnrollmentTypesAsync(), t => t.Id == id);
    }

    [Fact]
    public async Task GetGuardianRelationshipsForStudentAsync_returns_only_that_students_relationships()
    {
        await using var db = CreateContext();
        var studentPersonId = Guid.NewGuid();
        var otherStudentPersonId = Guid.NewGuid();
        db.GuardianStudentRelationships.Add(new GuardianStudentRelationship
        {
            Id = Guid.NewGuid(), GuardianPersonId = Guid.NewGuid(), StudentPersonId = studentPersonId,
            RelationshipTypeId = Guid.NewGuid(), EffectiveFrom = new DateOnly(2024, 1, 1),
        });
        db.GuardianStudentRelationships.Add(new GuardianStudentRelationship
        {
            Id = Guid.NewGuid(), GuardianPersonId = Guid.NewGuid(), StudentPersonId = otherStudentPersonId,
            RelationshipTypeId = Guid.NewGuid(), EffectiveFrom = new DateOnly(2024, 1, 1),
        });
        await db.SaveChangesAsync();
        var service = new PeopleAdminService(db);

        var relationships = await service.GetGuardianRelationshipsForStudentAsync(studentPersonId);

        Assert.Single(relationships, r => r.StudentPersonId == studentPersonId);
    }
}
