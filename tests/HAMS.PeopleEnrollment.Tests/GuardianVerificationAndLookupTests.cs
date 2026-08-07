using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Domain;
using HAMS.PeopleEnrollment.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.PeopleEnrollment.Tests;

public class GuardianVerificationAndLookupTests
{
    private static readonly Guid RelationshipTypeId = Guid.NewGuid();

    private static PeopleDbContext CreateContext() => new(
        new DbContextOptionsBuilder<PeopleDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Person CreatePerson(string? phoneNumber = null) => new()
    {
        Id = Guid.NewGuid(),
        NameEn = "Test Person",
        NameDv = "ޓެސްޓް",
        DateOfBirth = new DateOnly(1990, 1, 1),
        PhoneNumber = phoneNumber,
        Address = new Address
        {
            IslandId = Guid.NewGuid(), RoadEn = "Road", RoadDv = "Road", HouseNameEn = "House", HouseNameDv = "House",
        },
    };

    private static async Task<Guid> EstablishAsync(
        PeopleDbContext db, Guid guardianId, Guid studentId, bool canViewAcademicRecords = true, bool canViewAttendance = true,
        bool canViewInterventionUpdates = true)
    {
        var service = new GuardianRelationshipService(db);
        return await service.EstablishAsync(new EstablishGuardianRelationshipRequest(
            guardianId, studentId, RelationshipTypeId, HasLegalAuthority: true,
            canViewAcademicRecords, canViewAttendance, CanViewBehaviourRecords: false, canViewInterventionUpdates,
            CanReceiveNotifications: true, RestrictionTypeId: null, EffectiveFrom: new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public async Task VerifyAsync_moves_a_Pending_relationship_to_Verified()
    {
        await using var db = CreateContext();
        var id = await EstablishAsync(db, Guid.NewGuid(), Guid.NewGuid());
        var service = new GuardianRelationshipService(db);

        await service.VerifyAsync(id);

        var relationship = await db.GuardianStudentRelationships.SingleAsync(r => r.Id == id);
        Assert.Equal(GuardianVerificationStatus.Verified, relationship.VerificationStatus);
    }

    [Fact]
    public async Task RejectAsync_moves_a_Pending_relationship_to_Rejected()
    {
        await using var db = CreateContext();
        var id = await EstablishAsync(db, Guid.NewGuid(), Guid.NewGuid());
        var service = new GuardianRelationshipService(db);

        await service.RejectAsync(id);

        var relationship = await db.GuardianStudentRelationships.SingleAsync(r => r.Id == id);
        Assert.Equal(GuardianVerificationStatus.Rejected, relationship.VerificationStatus);
    }

    [Fact]
    public async Task VerifyAsync_throws_when_the_relationship_is_not_Pending()
    {
        await using var db = CreateContext();
        var id = await EstablishAsync(db, Guid.NewGuid(), Guid.NewGuid());
        var service = new GuardianRelationshipService(db);
        await service.VerifyAsync(id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.VerifyAsync(id));
    }

    [Fact]
    public async Task ReviseAsync_carries_forward_Verified_status_instead_of_resetting_to_Pending()
    {
        await using var db = CreateContext();
        var guardianId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var originalId = await EstablishAsync(db, guardianId, studentId);
        var service = new GuardianRelationshipService(db);
        await service.VerifyAsync(originalId);

        var revisedId = await service.ReviseAsync(
            originalId,
            new ReviseGuardianRelationshipRequest(
                RelationshipTypeId, HasLegalAuthority: true, CanViewAcademicRecords: true, CanViewAttendance: true,
                CanViewBehaviourRecords: false, CanViewInterventionUpdates: true, CanReceiveNotifications: true, RestrictionTypeId: null),
            effectiveFrom: new DateOnly(2026, 6, 1));

        var revised = await db.GuardianStudentRelationships.SingleAsync(r => r.Id == revisedId);
        Assert.Equal(GuardianVerificationStatus.Verified, revised.VerificationStatus);
    }

    [Fact]
    public async Task FindVerifiedGuardianPersonIdByPhoneAsync_returns_null_for_an_unregistered_number()
    {
        await using var db = CreateContext();
        var service = new GuardianRelationshipService(db);

        var result = await service.FindVerifiedGuardianPersonIdByPhoneAsync("+9609999999", new DateOnly(2026, 8, 5));

        Assert.Null(result);
    }

    [Fact]
    public async Task FindVerifiedGuardianPersonIdByPhoneAsync_returns_null_when_the_relationship_is_still_Pending()
    {
        await using var db = CreateContext();
        var guardian = CreatePerson("+9609701776");
        db.People.Add(guardian);
        await EstablishAsync(db, guardian.Id, Guid.NewGuid());
        var service = new GuardianRelationshipService(db);

        var result = await service.FindVerifiedGuardianPersonIdByPhoneAsync("+9609701776", new DateOnly(2026, 8, 5));

        Assert.Null(result);
    }

    [Fact]
    public async Task FindVerifiedGuardianPersonIdByPhoneAsync_returns_the_guardian_once_Verified()
    {
        await using var db = CreateContext();
        var guardian = CreatePerson("+9609701776");
        db.People.Add(guardian);
        var relationshipId = await EstablishAsync(db, guardian.Id, Guid.NewGuid());
        var service = new GuardianRelationshipService(db);
        await service.VerifyAsync(relationshipId);

        var result = await service.FindVerifiedGuardianPersonIdByPhoneAsync("+9609701776", new DateOnly(2026, 8, 5));

        Assert.Equal(guardian.Id, result);
    }

    [Fact]
    public async Task GetStudentsForGuardianAsync_returns_only_Verified_active_students_with_their_own_flags()
    {
        await using var db = CreateContext();
        var guardianId = Guid.NewGuid();
        var verifiedStudentId = Guid.NewGuid();
        var pendingStudentId = Guid.NewGuid();
        var service = new GuardianRelationshipService(db);

        var verifiedRelationshipId = await EstablishAsync(
            db, guardianId, verifiedStudentId, canViewAcademicRecords: true, canViewAttendance: false, canViewInterventionUpdates: true);
        await service.VerifyAsync(verifiedRelationshipId);
        await EstablishAsync(db, guardianId, pendingStudentId); // left Pending deliberately

        var students = await service.GetStudentsForGuardianAsync(guardianId, new DateOnly(2026, 8, 5));

        var summary = Assert.Single(students);
        Assert.Equal(verifiedStudentId, summary.StudentPersonId);
        Assert.True(summary.CanViewAcademicRecords);
        Assert.False(summary.CanViewAttendance);
        Assert.True(summary.CanViewInterventionUpdates);
    }

    [Fact]
    public async Task GetStudentsForGuardianAsync_resolves_the_student_name_and_admission_number()
    {
        await using var db = CreateContext();
        var guardianId = Guid.NewGuid();
        var student = CreatePerson();
        db.People.Add(student);
        db.StudentProfiles.Add(new StudentProfile { Id = Guid.NewGuid(), PersonId = student.Id, AdmissionNumber = "A2026-042", AdmissionDate = new DateOnly(2026, 1, 1) });
        var relationshipId = await EstablishAsync(db, guardianId, student.Id);
        var service = new GuardianRelationshipService(db);
        await service.VerifyAsync(relationshipId);

        var students = await service.GetStudentsForGuardianAsync(guardianId, new DateOnly(2026, 8, 5));

        var summary = Assert.Single(students);
        Assert.Equal("Test Person", summary.NameEn);
        Assert.Equal("ޓެސްޓް", summary.NameDv);
        Assert.Equal("A2026-042", summary.AdmissionNumber);
    }

    [Fact]
    public async Task GetStudentsForGuardianAsync_still_returns_the_relationship_when_no_profile_row_exists()
    {
        await using var db = CreateContext();
        var guardianId = Guid.NewGuid();
        var studentId = Guid.NewGuid(); // deliberately no Person/StudentProfile row seeded
        var relationshipId = await EstablishAsync(db, guardianId, studentId);
        var service = new GuardianRelationshipService(db);
        await service.VerifyAsync(relationshipId);

        var students = await service.GetStudentsForGuardianAsync(guardianId, new DateOnly(2026, 8, 5));

        var summary = Assert.Single(students);
        Assert.Equal(studentId, summary.StudentPersonId);
        Assert.Equal("", summary.NameEn);
        Assert.Equal("", summary.AdmissionNumber);
    }
}
