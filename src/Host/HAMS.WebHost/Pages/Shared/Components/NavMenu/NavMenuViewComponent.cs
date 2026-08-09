using HAMS.IdentityAccess.Application.Jwt;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace HAMS.WebHost.Pages.Shared.Components.NavMenu;

/// <summary>
/// Razor Pages equivalent of the former Blazor <c>NavMenu.razor</c> - same role-gating logic
/// (an admin group, a staff group, a regulatory group admin-only), just resolved from
/// <c>HttpContext.User</c> directly instead of a cascaded <c>AuthenticationState</c>.
/// </summary>
public sealed class NavMenuViewComponent(IRoleMembershipQuery roleMembershipQuery, IClock clock) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = HttpContext.User;
        var isStaff = user.HasClaim(HamsClaimTypes.IsStaff, "true");
        var isAdmin = false;

        var personIdValue = user.FindFirst(HamsClaimTypes.PersonId)?.Value;
        if (Guid.TryParse(personIdValue, out var personId))
        {
            isAdmin = await roleMembershipQuery.IsSystemOrSchoolAdminAsync(personId, clock.TodayUtc);
        }

        return View(new NavMenuModel(isAdmin, isStaff));
    }
}

public sealed record NavMenuModel(bool IsAdmin, bool IsStaff);
