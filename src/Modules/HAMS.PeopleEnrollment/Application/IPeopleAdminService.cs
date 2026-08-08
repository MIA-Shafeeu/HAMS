using HAMS.PeopleEnrollment.Domain;

namespace HAMS.PeopleEnrollment.Application;

/// <summary>A student profile joined with its <see cref="Person"/> name fields — the first "list students" read anywhere in this codebase (a Blazor admin UI is the first consumer that needs one; every prior caller already knew the specific student it wanted).</summary>
public sealed record StudentProfileSummary(Guid PersonId, string NameEn, string NameDv, string AdmissionNumber, DateOnly AdmissionDate);

/// <summary>A staff profile joined with its <see cref="Person"/> name fields. <see cref="Id"/> is the <see cref="StaffProfile"/>'s own id — <see cref="StaffQualification.StaffProfileId"/> keys off this, not <see cref="PersonId"/>.</summary>
public sealed record StaffProfileSummary(Guid Id, Guid PersonId, string NameEn, string NameDv, string EmployeeNumber, DateOnly HireDate, Guid EmploymentStatusId);

/// <summary>A guardian profile joined with its <see cref="Person"/> name fields.</summary>
public sealed record GuardianProfileSummary(Guid PersonId, string NameEn, string NameDv);

/// <summary>
/// Student/Guardian/Staff master-data setup (build plan Phase 3 scope) — extracted from what had
/// been purely inline <c>PeopleDbContext</c> queries directly inside <c>PeopleEndpoints</c>' minimal-API
/// lambdas, the same extraction already done for <c>IOrgAdminService</c>/<c>ICurriculumAdminService</c>.
/// <see cref="IGuardianRelationshipService"/>/<see cref="IStudentEnrollmentService"/> already existed
/// and are deliberately NOT duplicated here — this service only covers what had no service at all.
/// </summary>
public interface IPeopleAdminService
{
    Task<Guid> CreateAtollAsync(string code, string nameEn, string? nameDv, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Atoll>> GetAtollsAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateIslandAsync(Guid atollId, string code, string nameEn, string? nameDv, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Island>> GetIslandsAsync(Guid atollId, CancellationToken cancellationToken = default);

    Task<Guid> CreatePersonAsync(string nameEn, string nameDv, DateOnly dateOfBirth, Address address, string? phoneNumber, string? email, CancellationToken cancellationToken = default);

    Task<Person?> GetPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<Guid> CreateStudentProfileAsync(Guid personId, string admissionNumber, DateOnly admissionDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentProfileSummary>> GetStudentProfilesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmploymentStatus>> GetEmploymentStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>Every <c>EmploymentStatus</c>, active or not — the Reference Data admin screen's list, distinct from <see cref="GetEmploymentStatusesAsync"/>'s active-only picker list.</summary>
    Task<IReadOnlyList<EmploymentStatus>> GetAllEmploymentStatusesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateEmploymentStatusAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No employment status with that id exists.</exception>
    Task SetEmploymentStatusActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No employment status with that id exists.</exception>
    Task UpdateEmploymentStatusAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No active employment status with that code exists.</exception>
    Task<Guid> CreateStaffProfileAsync(Guid personId, string employeeNumber, DateOnly hireDate, string employmentStatusCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffProfileSummary>> GetStaffProfilesAsync(CancellationToken cancellationToken = default);

    Task<Guid> AddStaffQualificationAsync(Guid staffProfileId, string title, string? awardingInstitution, int? yearAwarded, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffQualification>> GetStaffQualificationsAsync(Guid staffProfileId, CancellationToken cancellationToken = default);

    Task<Guid> CreateGuardianProfileAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GuardianProfileSummary>> GetGuardianProfilesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RelationshipType>> GetRelationshipTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>Every <c>RelationshipType</c>, active or not — the Reference Data admin screen's list, distinct from <see cref="GetRelationshipTypesAsync"/>'s active-only picker list.</summary>
    Task<IReadOnlyList<RelationshipType>> GetAllRelationshipTypesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateRelationshipTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No relationship type with that id exists.</exception>
    Task SetRelationshipTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No relationship type with that id exists.</exception>
    Task UpdateRelationshipTypeAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RestrictionType>> GetRestrictionTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>Every <c>RestrictionType</c>, active or not — the Reference Data admin screen's list, distinct from <see cref="GetRestrictionTypesAsync"/>'s active-only picker list.</summary>
    Task<IReadOnlyList<RestrictionType>> GetAllRestrictionTypesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateRestrictionTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No restriction type with that id exists.</exception>
    Task SetRestrictionTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No restriction type with that id exists.</exception>
    Task UpdateRestrictionTypeAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrollmentType>> GetEnrollmentTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>Every <c>EnrollmentType</c>, active or not — the Reference Data admin screen's list, distinct from <see cref="GetEnrollmentTypesAsync"/>'s active-only picker list.</summary>
    Task<IReadOnlyList<EnrollmentType>> GetAllEnrollmentTypesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateEnrollmentTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No enrollment type with that id exists.</exception>
    Task SetEnrollmentTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No enrollment type with that id exists.</exception>
    Task UpdateEnrollmentTypeAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GuardianStudentRelationship>> GetGuardianRelationshipsForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentEnrollment>> GetEnrollmentsForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default);
}
