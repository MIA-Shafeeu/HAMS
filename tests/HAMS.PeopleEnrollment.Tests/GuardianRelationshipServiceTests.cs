using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.PeopleEnrollment.Tests;

public class GuardianRelationshipServiceTests
{
    private static readonly Guid RelationshipTypeId = Guid.NewGuid();

    private static PeopleDbContext CreateContext() => new(
        new DbContextOptionsBuilder<PeopleDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task EstablishAsync_creates_an_open_ended_active_relationship()
    {
        await using var db = CreateContext();
        var service = new GuardianRelationshipService(db);
        var guardianId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var id = await service.EstablishAsync(new EstablishGuardianRelationshipRequest(
            guardianId, studentId, RelationshipTypeId, HasLegalAuthority: true,
            CanViewAcademicRecords: true, CanViewAttendance: true, CanViewBehaviourRecords: false, CanViewInterventionUpdates: false,
            CanReceiveNotifications: true, RestrictionTypeId: null, EffectiveFrom: new DateOnly(2026, 1, 1)));

        var relationship = await db.GuardianStudentRelationships.SingleAsync(r => r.Id == id);
        Assert.Null(relationship.EffectiveTo);
        Assert.True(relationship.HasLegalAuthority);
    }

    [Fact]
    public async Task ReviseAsync_closes_the_old_row_and_opens_a_new_one_preserving_history()
    {
        await using var db = CreateContext();
        var service = new GuardianRelationshipService(db);
        var guardianId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var originalId = await service.EstablishAsync(new EstablishGuardianRelationshipRequest(
            guardianId, studentId, RelationshipTypeId, HasLegalAuthority: true,
            CanViewAcademicRecords: true, CanViewAttendance: true, CanViewBehaviourRecords: false, CanViewInterventionUpdates: false,
            CanReceiveNotifications: true, RestrictionTypeId: null, EffectiveFrom: new DateOnly(2026, 1, 1)));

        var revisedId = await service.ReviseAsync(
            originalId,
            new ReviseGuardianRelationshipRequest(
                RelationshipTypeId, HasLegalAuthority: true,
                CanViewAcademicRecords: true, CanViewAttendance: true, CanViewBehaviourRecords: true, CanViewInterventionUpdates: false,
                CanReceiveNotifications: true, RestrictionTypeId: null),
            effectiveFrom: new DateOnly(2026, 6, 1));

        var original = await db.GuardianStudentRelationships.AsNoTracking().SingleAsync(r => r.Id == originalId);
        var revised = await db.GuardianStudentRelationships.AsNoTracking().SingleAsync(r => r.Id == revisedId);

        // Old row: closed, but its historical values are completely untouched.
        Assert.Equal(new DateOnly(2026, 5, 31), original.EffectiveTo);
        Assert.False(original.CanViewBehaviourRecords);

        // New row: takes over from the day the old one closed, with the revised permissions.
        Assert.Equal(new DateOnly(2026, 6, 1), revised.EffectiveFrom);
        Assert.Null(revised.EffectiveTo);
        Assert.True(revised.CanViewBehaviourRecords);

        Assert.NotEqual(originalId, revisedId);
    }

    [Fact]
    public async Task ReviseAsync_throws_if_the_relationship_is_already_closed()
    {
        await using var db = CreateContext();
        var service = new GuardianRelationshipService(db);
        var id = await service.EstablishAsync(new EstablishGuardianRelationshipRequest(
            Guid.NewGuid(), Guid.NewGuid(), RelationshipTypeId, HasLegalAuthority: true,
            CanViewAcademicRecords: true, CanViewAttendance: true, CanViewBehaviourRecords: false, CanViewInterventionUpdates: false,
            CanReceiveNotifications: true, RestrictionTypeId: null, EffectiveFrom: new DateOnly(2026, 1, 1)));

        await service.CloseAsync(id, new DateOnly(2026, 3, 1));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviseAsync(
            id,
            new ReviseGuardianRelationshipRequest(RelationshipTypeId, true, true, true, true, false, true, null),
            new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public async Task CloseAsync_ends_the_relationship_with_no_replacement()
    {
        await using var db = CreateContext();
        var service = new GuardianRelationshipService(db);
        var id = await service.EstablishAsync(new EstablishGuardianRelationshipRequest(
            Guid.NewGuid(), Guid.NewGuid(), RelationshipTypeId, HasLegalAuthority: true,
            CanViewAcademicRecords: true, CanViewAttendance: true, CanViewBehaviourRecords: false, CanViewInterventionUpdates: false,
            CanReceiveNotifications: true, RestrictionTypeId: null, EffectiveFrom: new DateOnly(2026, 1, 1)));

        await service.CloseAsync(id, new DateOnly(2026, 3, 1));

        var relationship = await db.GuardianStudentRelationships.SingleAsync(r => r.Id == id);
        Assert.Equal(new DateOnly(2026, 3, 1), relationship.EffectiveTo);

        var totalRows = await db.GuardianStudentRelationships.CountAsync();
        Assert.Equal(1, totalRows);
    }
}
