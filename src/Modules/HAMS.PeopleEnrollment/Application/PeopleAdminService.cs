using HAMS.PeopleEnrollment.Domain;
using HAMS.PeopleEnrollment.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.PeopleEnrollment.Application;

internal sealed class PeopleAdminService(PeopleDbContext dbContext) : IPeopleAdminService
{
    public async Task<Guid> CreateAtollAsync(string code, string nameEn, string? nameDv, int displayOrder, CancellationToken cancellationToken = default)
    {
        var atoll = new Atoll { Id = Guid.NewGuid(), Code = code, NameEn = nameEn, NameDv = nameDv, DisplayOrder = displayOrder };
        dbContext.Atolls.Add(atoll);
        await dbContext.SaveChangesAsync(cancellationToken);
        return atoll.Id;
    }

    public async Task<IReadOnlyList<Atoll>> GetAtollsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Atolls.OrderBy(a => a.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateIslandAsync(Guid atollId, string code, string nameEn, string? nameDv, int displayOrder, CancellationToken cancellationToken = default)
    {
        var island = new Island { Id = Guid.NewGuid(), AtollId = atollId, Code = code, NameEn = nameEn, NameDv = nameDv, DisplayOrder = displayOrder };
        dbContext.Islands.Add(island);
        await dbContext.SaveChangesAsync(cancellationToken);
        return island.Id;
    }

    public async Task<IReadOnlyList<Island>> GetIslandsAsync(Guid atollId, CancellationToken cancellationToken = default) =>
        await dbContext.Islands.Where(i => i.AtollId == atollId).OrderBy(i => i.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreatePersonAsync(string nameEn, string nameDv, DateOnly dateOfBirth, Address address, string? phoneNumber, string? email, CancellationToken cancellationToken = default)
    {
        var person = new Person
        {
            Id = Guid.NewGuid(), NameEn = nameEn, NameDv = nameDv, DateOfBirth = dateOfBirth,
            Address = address, PhoneNumber = phoneNumber, Email = email,
        };
        dbContext.People.Add(person);
        await dbContext.SaveChangesAsync(cancellationToken);
        return person.Id;
    }

    public async Task<Person?> GetPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
        await dbContext.People.SingleOrDefaultAsync(p => p.Id == personId, cancellationToken);

    public async Task<Guid> CreateStudentProfileAsync(Guid personId, string admissionNumber, DateOnly admissionDate, CancellationToken cancellationToken = default)
    {
        var profile = new StudentProfile { Id = Guid.NewGuid(), PersonId = personId, AdmissionNumber = admissionNumber, AdmissionDate = admissionDate };
        dbContext.StudentProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile.Id;
    }

    public async Task<IReadOnlyList<StudentProfileSummary>> GetStudentProfilesAsync(CancellationToken cancellationToken = default)
    {
        var query =
            from profile in dbContext.StudentProfiles
            join person in dbContext.People on profile.PersonId equals person.Id
            orderby person.NameEn
            select new { profile.PersonId, person.NameEn, person.NameDv, profile.AdmissionNumber, profile.AdmissionDate };

        var rows = await query.ToListAsync(cancellationToken);
        return rows.Select(r => new StudentProfileSummary(r.PersonId, r.NameEn, r.NameDv, r.AdmissionNumber, r.AdmissionDate)).ToList();
    }

    public async Task<IReadOnlyList<EmploymentStatus>> GetEmploymentStatusesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.EmploymentStatuses.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EmploymentStatus>> GetAllEmploymentStatusesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.EmploymentStatuses.OrderBy(s => s.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateEmploymentStatusAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var status = new EmploymentStatus { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.EmploymentStatuses.Add(status);
        await dbContext.SaveChangesAsync(cancellationToken);
        return status.Id;
    }

    public async Task SetEmploymentStatusActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var status = await dbContext.EmploymentStatuses.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Employment status '{id}' not found.");
        status.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateStaffProfileAsync(Guid personId, string employeeNumber, DateOnly hireDate, string employmentStatusCode, CancellationToken cancellationToken = default)
    {
        var status = await dbContext.EmploymentStatuses.SingleOrDefaultAsync(s => s.Code == employmentStatusCode && s.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active employment status with code '{employmentStatusCode}'.");

        var profile = new StaffProfile { Id = Guid.NewGuid(), PersonId = personId, EmployeeNumber = employeeNumber, HireDate = hireDate, EmploymentStatusId = status.Id };
        dbContext.StaffProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile.Id;
    }

    public async Task<IReadOnlyList<StaffProfileSummary>> GetStaffProfilesAsync(CancellationToken cancellationToken = default)
    {
        var query =
            from profile in dbContext.StaffProfiles
            join person in dbContext.People on profile.PersonId equals person.Id
            orderby person.NameEn
            select new { profile.Id, profile.PersonId, person.NameEn, person.NameDv, profile.EmployeeNumber, profile.HireDate, profile.EmploymentStatusId };

        var rows = await query.ToListAsync(cancellationToken);
        return rows.Select(r => new StaffProfileSummary(r.Id, r.PersonId, r.NameEn, r.NameDv, r.EmployeeNumber, r.HireDate, r.EmploymentStatusId)).ToList();
    }

    public async Task<Guid> AddStaffQualificationAsync(Guid staffProfileId, string title, string? awardingInstitution, int? yearAwarded, CancellationToken cancellationToken = default)
    {
        var qualification = new StaffQualification { Id = Guid.NewGuid(), StaffProfileId = staffProfileId, Title = title, AwardingInstitution = awardingInstitution, YearAwarded = yearAwarded };
        dbContext.StaffQualifications.Add(qualification);
        await dbContext.SaveChangesAsync(cancellationToken);
        return qualification.Id;
    }

    public async Task<IReadOnlyList<StaffQualification>> GetStaffQualificationsAsync(Guid staffProfileId, CancellationToken cancellationToken = default) =>
        await dbContext.StaffQualifications.Where(q => q.StaffProfileId == staffProfileId).ToListAsync(cancellationToken);

    public async Task<Guid> CreateGuardianProfileAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var profile = new GuardianProfile { Id = Guid.NewGuid(), PersonId = personId };
        dbContext.GuardianProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile.Id;
    }

    public async Task<IReadOnlyList<GuardianProfileSummary>> GetGuardianProfilesAsync(CancellationToken cancellationToken = default)
    {
        var query =
            from profile in dbContext.GuardianProfiles
            join person in dbContext.People on profile.PersonId equals person.Id
            orderby person.NameEn
            select new { profile.PersonId, person.NameEn, person.NameDv };

        var rows = await query.ToListAsync(cancellationToken);
        return rows.Select(r => new GuardianProfileSummary(r.PersonId, r.NameEn, r.NameDv)).ToList();
    }

    public async Task<IReadOnlyList<RelationshipType>> GetRelationshipTypesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.RelationshipTypes.Where(t => t.IsActive).OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RelationshipType>> GetAllRelationshipTypesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.RelationshipTypes.OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateRelationshipTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var relationshipType = new RelationshipType { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.RelationshipTypes.Add(relationshipType);
        await dbContext.SaveChangesAsync(cancellationToken);
        return relationshipType.Id;
    }

    public async Task SetRelationshipTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var relationshipType = await dbContext.RelationshipTypes.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Relationship type '{id}' not found.");
        relationshipType.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RestrictionType>> GetRestrictionTypesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.RestrictionTypes.Where(t => t.IsActive).OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RestrictionType>> GetAllRestrictionTypesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.RestrictionTypes.OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateRestrictionTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var restrictionType = new RestrictionType { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.RestrictionTypes.Add(restrictionType);
        await dbContext.SaveChangesAsync(cancellationToken);
        return restrictionType.Id;
    }

    public async Task SetRestrictionTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var restrictionType = await dbContext.RestrictionTypes.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Restriction type '{id}' not found.");
        restrictionType.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EnrollmentType>> GetEnrollmentTypesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.EnrollmentTypes.Where(t => t.IsActive).OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EnrollmentType>> GetAllEnrollmentTypesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.EnrollmentTypes.OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateEnrollmentTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var enrollmentType = new EnrollmentType { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.EnrollmentTypes.Add(enrollmentType);
        await dbContext.SaveChangesAsync(cancellationToken);
        return enrollmentType.Id;
    }

    public async Task SetEnrollmentTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var enrollmentType = await dbContext.EnrollmentTypes.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Enrollment type '{id}' not found.");
        enrollmentType.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GuardianStudentRelationship>> GetGuardianRelationshipsForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default) =>
        await dbContext.GuardianStudentRelationships.Where(r => r.StudentPersonId == studentPersonId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StudentEnrollment>> GetEnrollmentsForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default) =>
        await dbContext.StudentEnrollments.Where(e => e.StudentPersonId == studentPersonId).OrderByDescending(e => e.EffectiveFrom).ToListAsync(cancellationToken);
}
