using HAMS.Platform.Notifications.Application;
using HAMS.Platform.Notifications.Domain;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Admin;

[Authorize(Policy = SystemOrSchoolAdminPolicy.Name)]
public sealed class NotificationMonitorModel(INotificationAdminService notificationAdmin) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public NotificationDeliveryStatus? Status { get; set; }

    public IReadOnlyList<NotificationOutboxSummary> Entries { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Entries = await notificationAdmin.GetEntriesAsync(Status);
    }

    public async Task<IActionResult> OnPostRetryAsync(Guid id)
    {
        try
        {
            await notificationAdmin.RetryAsync(id);
            TempData["FlashMessage"] = "Notification re-queued for delivery.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return RedirectToPage(new { Status });
    }
}
