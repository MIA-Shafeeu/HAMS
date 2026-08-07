using HAMS.PeopleEnrollment.Domain;
using HAMS.PeopleEnrollment.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.PeopleEnrollment.Application;

internal sealed class GuardianRelationshipService(PeopleDbContext dbContext) : IGuardianRelationshipService
{
    public async Task<Guid> EstablishAsync(EstablishGuardianRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        var relationship = new GuardianStudentRelationship
        {
            Id = Guid.NewGuid(),
            GuardianPersonId = request.GuardianPersonId,
            StudentPersonId = request.StudentPersonId,
            RelationshipTypeId = request.RelationshipTypeId,
            HasLegalAuthority = request.HasLegalAuthority,
            CanViewAcademicRecords = request.CanViewAcademicRecords,
            CanViewAttendance = request.CanViewAttendance,
            CanViewBehaviourRecords = request.CanViewBehaviourRecords,
            CanViewInterventionUpdates = request.CanViewInterventionUpdates,
            CanReceiveNotifications = request.CanReceiveNotifications,
            RestrictionTypeId = request.RestrictionTypeId,
            EffectiveFrom = request.EffectiveFrom,
        };

        dbContext.GuardianStudentRelationships.Add(relationship);
        await dbContext.SaveChangesAsync(cancellationToken);

        return relationship.Id;
    }

    public async Task<Guid> ReviseAsync(
        Guid currentRelationshipId, ReviseGuardianRelationshipRequest request, DateOnly effectiveFrom, CancellationToken cancellationToken = default)
    {
        var current = await dbContext.GuardianStudentRelationships.FindAsync([currentRelationshipId], cancellationToken)
            ?? throw new InvalidOperationException("Guardian relationship not found.");

        if (current.EffectiveTo is not null)
        {
            throw new InvalidOperationException("This relationship has already been closed.");
        }

        current.EffectiveTo = effectiveFrom.AddDays(-1);

        var revised = new GuardianStudentRelationship
        {
            Id = Guid.NewGuid(),
            GuardianPersonId = current.GuardianPersonId,
            StudentPersonId = current.StudentPersonId,
            RelationshipTypeId = request.RelationshipTypeId,
            HasLegalAuthority = request.HasLegalAuthority,
            CanViewAcademicRecords = request.CanViewAcademicRecords,
            CanViewAttendance = request.CanViewAttendance,
            CanViewBehaviourRecords = request.CanViewBehaviourRecords,
            CanViewInterventionUpdates = request.CanViewInterventionUpdates,
            CanReceiveNotifications = request.CanReceiveNotifications,
            // Carries forward, never resets to Pending — a revision changes permissions/relationship
            // type, not the underlying identity verification a school already confirmed. Resetting
            // this would silently re-lock an already-Verified guardian out of the portal every time
            // an admin merely tweaks a Can-View flag.
            VerificationStatus = current.VerificationStatus,
            RestrictionTypeId = request.RestrictionTypeId,
            EffectiveFrom = effectiveFrom,
        };

        dbContext.GuardianStudentRelationships.Add(revised);
        await dbContext.SaveChangesAsync(cancellationToken);

        return revised.Id;
    }

    public async Task CloseAsync(Guid relationshipId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
    {
        var relationship = await dbContext.GuardianStudentRelationships.FindAsync([relationshipId], cancellationToken)
            ?? throw new InvalidOperationException("Guardian relationship not found.");

        relationship.EffectiveTo = effectiveTo;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task VerifyAsync(Guid relationshipId, CancellationToken cancellationToken = default)
    {
        var relationship = await GetPendingAsync(relationshipId, cancellationToken);
        relationship.VerificationStatus = GuardianVerificationStatus.Verified;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid relationshipId, CancellationToken cancellationToken = default)
    {
        var relationship = await GetPendingAsync(relationshipId, cancellationToken);
        relationship.VerificationStatus = GuardianVerificationStatus.Rejected;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> FindVerifiedGuardianPersonIdByPhoneAsync(
        string phoneNumber, DateOnly asOf, CancellationToken cancellationToken = default)
        => await (
            from person in dbContext.People
            where person.PhoneNumber == phoneNumber && person.IsActive
            join relationship in dbContext.GuardianStudentRelationships
                .Where(r => r.VerificationStatus == GuardianVerificationStatus.Verified)
                .ActiveAsOf(asOf)
                on person.Id equals relationship.GuardianPersonId
            select (Guid?)person.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<GuardianStudentSummary>> GetStudentsForGuardianAsync(
        Guid guardianPersonId, DateOnly asOf, CancellationToken cancellationToken = default)
        // Left joins (not inner) against Person/StudentProfile: which students a guardian may see is
        // authoritative from the relationship row alone — a missing profile row must never make an
        // otherwise-Verified relationship silently disappear from the list, it should just resolve to
        // an empty display name/admission number.
        => await dbContext.GuardianStudentRelationships
            .Where(r => r.GuardianPersonId == guardianPersonId && r.VerificationStatus == GuardianVerificationStatus.Verified)
            .ActiveAsOf(asOf)
            .GroupJoin(dbContext.People, r => r.StudentPersonId, p => p.Id, (r, people) => new { r, people })
            .SelectMany(x => x.people.DefaultIfEmpty(), (x, person) => new { x.r, person })
            .GroupJoin(dbContext.StudentProfiles, x => x.r.StudentPersonId, sp => sp.PersonId, (x, profiles) => new { x.r, x.person, profiles })
            .SelectMany(x => x.profiles.DefaultIfEmpty(), (x, profile) => new GuardianStudentSummary(
                x.r.StudentPersonId, x.r.CanViewAcademicRecords, x.r.CanViewAttendance, x.r.CanViewInterventionUpdates,
                x.r.CanViewBehaviourRecords,
                x.person != null ? x.person.NameEn : "",
                x.person != null ? x.person.NameDv : "",
                profile != null ? profile.AdmissionNumber : ""))
            .ToListAsync(cancellationToken);

    private async Task<GuardianStudentRelationship> GetPendingAsync(Guid relationshipId, CancellationToken cancellationToken)
    {
        var relationship = await dbContext.GuardianStudentRelationships.FindAsync([relationshipId], cancellationToken)
            ?? throw new InvalidOperationException("Guardian relationship not found.");

        if (relationship.VerificationStatus != GuardianVerificationStatus.Pending)
        {
            throw new InvalidOperationException($"This relationship is already {relationship.VerificationStatus}, not Pending.");
        }

        return relationship;
    }
}
