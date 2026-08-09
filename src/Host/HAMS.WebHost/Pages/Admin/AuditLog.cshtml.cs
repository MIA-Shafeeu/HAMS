using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Admin;

[Authorize(Policy = SystemOrSchoolAdminPolicy.Name)]
public sealed class AuditLogModel(IAuditLogQuery auditLogQuery) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateOnly? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public AuditAction? Action { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchText { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 25;

    public IReadOnlyList<string> EntityTypes { get; private set; } = [];
    public IReadOnlyList<AuditLogEntry> Entries { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public async Task OnGetAsync()
    {
        EntityTypes = await auditLogQuery.GetDistinctEntityTypesAsync();

        var request = new AuditLogSearchRequest(
            FromUtc: FromDate is { } from ? new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null,
            ToUtc: ToDate is { } to ? new DateTimeOffset(to.ToDateTime(TimeOnly.MinValue).AddDays(1), TimeSpan.Zero) : null,
            Action: Action,
            EntityType: string.IsNullOrWhiteSpace(EntityType) ? null : EntityType,
            SearchText: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
            Page: PageNumber,
            PageSize: PageSize);

        var result = await auditLogQuery.SearchAsync(request);
        Entries = result.Entries;
        TotalCount = result.TotalCount;
    }

    public static string ActionBadgeClass(AuditAction action) => action switch
    {
        AuditAction.Create => "badge text-bg-success",
        AuditAction.Update => "badge text-bg-info",
        AuditAction.Delete => "badge text-bg-danger",
        AuditAction.PermissionDenied => "badge text-bg-danger",
        AuditAction.LoginFailed => "badge text-bg-warning",
        AuditAction.Login => "badge text-bg-success",
        AuditAction.Logout => "badge text-bg-secondary",
        AuditAction.Read => "badge text-bg-secondary",
        _ => "badge text-bg-secondary",
    };
}
