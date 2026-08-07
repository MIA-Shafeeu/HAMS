using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Notifications.Application;
using HAMS.Platform.Notifications.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.Platform.Notifications.Endpoints;

/// <summary>
/// Notification Outbox Monitor admin surface (build plan Phase D). System/School Administrator only
/// — this exposes delivery status and raw error text for every recipient in the queue. Public
/// (unlike every business module's own internal Endpoints classes) because Notifications is a
/// Platform kernel with no owning module wrapper to call this from within the same assembly —
/// `HAMS.WebHost/Program.cs` maps it directly.
/// </summary>
public static class NotificationAdminEndpoints
{
    public static IEndpointRouteBuilder MapNotificationAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/notifications").WithTags("NotificationAdmin").RequireAuthorization();

        group.MapGet("/", async (
            NotificationDeliveryStatus? status, int? take, INotificationAdminService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            return Results.Ok(await service.GetEntriesAsync(status, take ?? 100, ct));
        });

        group.MapPost("/{entryId:guid}/retry", async (
            Guid entryId, INotificationAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.RetryAsync(entryId, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return endpoints;
    }
}
