using HAMS.IdentityAccess.Domain;
using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.IdentityAccess.Infrastructure;

/// <summary>
/// Creates a bootstrap System Administrator so there is someone able to sign in and grant every
/// other role — a real deployment has no other way to escape the "need an admin to create the
/// first admin" chicken-and-egg problem. Development-only by design: never auto-create a
/// known-password account in a production environment.
/// </summary>
public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var configuration = provider.GetRequiredService<IConfiguration>();
        var username = configuration["Bootstrap:AdminUsername"] ?? "admin";
        var password = configuration["Bootstrap:AdminPassword"]
            ?? throw new InvalidOperationException("Bootstrap:AdminPassword must be configured for development seeding.");

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        var existing = await userManager.FindByNameAsync(username);
        if (existing is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = username,
            Email = $"{username}@hirilandhoo.edu.mv",
            EmailConfirmed = true,
            PersonId = Guid.NewGuid(),
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed bootstrap admin: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
        }

        var assignmentService = provider.GetRequiredService<IPersonRoleAssignmentService>();
        await assignmentService.AssignRoleAsync(
            user.PersonId, RoleCodes.SystemAdministrator, schoolId: null,
            effectiveFrom: DateOnly.FromDateTime(DateTime.UtcNow), effectiveTo: null,
            cancellationToken: cancellationToken);
    }
}
