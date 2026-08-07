using HAMS.Platform.Access.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access.Infrastructure;

internal sealed class PersonRoleAssignmentService(AccessDbContext dbContext, IAccessGrantProjectionService projection)
    : IPersonRoleAssignmentService
{
    public async Task<Guid> AssignRoleAsync(
        Guid personId, string roleCode, Guid? schoolId, DateOnly effectiveFrom, DateOnly? effectiveTo,
        CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles.SingleOrDefaultAsync(r => r.Code == roleCode && r.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active role with code '{roleCode}'.");

        var assignment = new PersonRoleAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            RoleId = role.Id,
            SchoolId = schoolId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
        };
        dbContext.PersonRoleAssignments.Add(assignment);

        await projection.UpsertRoleGrantAsync(personId, role.Id, schoolId, effectiveFrom, effectiveTo, assignment.Id, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return assignment.Id;
    }

    public async Task RevokeRoleAsync(Guid personRoleAssignmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.PersonRoleAssignments.FindAsync([personRoleAssignmentId], cancellationToken)
            ?? throw new InvalidOperationException("Role assignment not found.");

        assignment.EffectiveTo = effectiveTo;

        await projection.CloseAsync(AccessGrantSourceTypes.PersonRoleAssignment, personRoleAssignmentId, effectiveTo, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Roles.Where(r => r.IsActive).OrderBy(r => r.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Roles.OrderBy(r => r.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PersonRoleAssignment>> GetAssignmentsForPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
        await dbContext.PersonRoleAssignments.Where(a => a.PersonId == personId).OrderByDescending(a => a.EffectiveFrom).ToListAsync(cancellationToken);

    public async Task<Guid> CreateRoleAsync(string code, string name, string? description, int displayOrder, CancellationToken cancellationToken = default)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            DisplayOrder = displayOrder,
        };
        dbContext.Roles.Add(role);

        await dbContext.SaveChangesAsync(cancellationToken);

        return role.Id;
    }

    public async Task SetRoleActiveAsync(Guid roleId, bool isActive, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles.FindAsync([roleId], cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        role.IsActive = isActive;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateConfidentialityTierAsync(string code, string name, string? description, int rank, int displayOrder, CancellationToken cancellationToken = default)
    {
        var tier = new ConfidentialityTier
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            Rank = rank,
            DisplayOrder = displayOrder,
        };
        dbContext.ConfidentialityTiers.Add(tier);

        await dbContext.SaveChangesAsync(cancellationToken);

        return tier.Id;
    }

    public async Task<IReadOnlyList<ConfidentialityTier>> GetConfidentialityTiersAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ConfidentialityTiers.OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

    public async Task SetConfidentialityTierActiveAsync(Guid tierId, bool isActive, CancellationToken cancellationToken = default)
    {
        var tier = await dbContext.ConfidentialityTiers.FindAsync([tierId], cancellationToken)
            ?? throw new InvalidOperationException("Confidentiality tier not found.");

        tier.IsActive = isActive;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
