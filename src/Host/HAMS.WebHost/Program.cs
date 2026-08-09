using HAMS.AdminIntegration;
using HAMS.AssessmentEvaluation;
using HAMS.AssessmentEvaluation.Infrastructure;
using HAMS.Attendance;
using HAMS.Attendance.Infrastructure;
using HAMS.CommunicationPortals;
using HAMS.CommunicationPortals.Infrastructure;
using HAMS.Fundraising;
using HAMS.IdentityAccess;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.IdentityAccess.Infrastructure;
using HAMS.Intervention;
using HAMS.Intervention.Infrastructure;
using HAMS.LearningDelivery;
using HAMS.LearningDelivery.Infrastructure;
using HAMS.OrgCurriculum;
using HAMS.OrgCurriculum.Infrastructure;
using HAMS.PeopleEnrollment;
using HAMS.PeopleEnrollment.Infrastructure;
using HAMS.Platform.Access;
using HAMS.Platform.Access.Infrastructure;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Infrastructure;
using HAMS.Platform.Common;
using HAMS.Platform.Documents;
using HAMS.Platform.Notifications;
using HAMS.Platform.Notifications.Endpoints;
using HAMS.Platform.Notifications.Infrastructure;
using HAMS.Platform.Workflow;
using HAMS.ReportingAnalyticsAudit;
using HAMS.ReportingAnalyticsAudit.Infrastructure;
using HAMS.TeachingTimetable;
using HAMS.TeachingTimetable.Infrastructure;
using HAMS.WebHost.Components;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Blazor Server's SignalR circuit was running on 100% framework defaults (15s keep-alive ping,
// 30s client timeout), tuned for a direct connection - this app instead sits behind a Cloudflare
// Tunnel hop, and real users are sometimes on flaky island/government-network connections. If
// anything in that path (the tunnel, an intermediate proxy) has an idle-connection timeout close
// to that 15s ping interval, or the round-trip is just slow, the circuit can silently drop and
// reconnect - which tears down and recreates the ENTIRE rendered DOM, explaining reports of every
// field (not just one component type) losing focus/input mid-interaction. Ping more often so the
// connection never looks idle to an intermediate hop, and give reconnection far more slack before
// giving up, so a several-second network hiccup reconnects silently instead of forcing a full
// page reload.
builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(5);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});
builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
{
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
    options.DisconnectedCircuitMaxRetained = 200;
    options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(60);
});

// Phase 12's Blazor Server admin UI: flows the Cookie-authenticated ClaimsPrincipal (see
// IdentityAccessModule's .AddCookie call) into every interactive component via the standard
// [CascadingParameter] Task<AuthenticationState> mechanism — deliberately NOT ICurrentUser's
// IHttpContextAccessor, which isn't reliably populated once a Server-render circuit is running.
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddMudServices();

// Real admin gating for the admin-only Blazor pages (Dashboard/Audit Log/Regulatory Reports) — see
// SystemOrSchoolAdminPolicy.cs's own remarks for why a bare [Authorize] wasn't enough and a
// scheme-restricted one doesn't work on a Razor component.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(SystemOrSchoolAdminPolicy.Name, policy => policy.Requirements.Add(SystemOrSchoolAdminRequirement.Instance))
    .AddPolicy(StaffPolicy.Name, policy => policy.RequireClaim(HamsClaimTypes.IsStaff, "true"));
builder.Services.AddScoped<IAuthorizationHandler, SystemOrSchoolAdminAuthorizationHandler>();

// Guardian/student portal (HAMS.WebHost.Client, Phase C1) services registered here too, even though
// they only ever really run client-side: Blazor Web App instantiates a WASM-rendered component's
// [Inject] properties via the SERVER's DI container once for every request (regardless of the
// component's own prerender setting), so anything a portal page injects must resolve here as well or
// that instantiation throws before the browser ever gets a chance to boot the real WASM runtime and
// its own separate DI container (see HAMS.WebHost.Client/Program.cs for the client-side registrations
// that actually get used). Deliberately NOT re-mapping AuthenticationStateProvider itself here — that
// would override the framework's cookie-based provider staff pages depend on; PortalAuthenticationStateProvider
// is registered only as its own concrete type, so <AuthorizeView> during this server-side pass still
// correctly resolves to "anonymous" (truthful — guardians/students never hold a staff auth cookie).
builder.Services.AddScoped<HAMS.WebHost.Client.Services.TokenStorage>();
builder.Services.AddScoped<HAMS.WebHost.Client.Services.PortalAuthenticationStateProvider>();
builder.Services.AddScoped<HAMS.WebHost.Client.Services.BearerTokenHandler>();
// Base address is never actually dialed from here — these named clients exist only to satisfy DI
// property injection during the server-side pass described above.
builder.Services.AddHttpClient("HAMS.Api.Refresh", client => client.BaseAddress = new Uri("http://localhost/"));
builder.Services.AddHttpClient("HAMS.Api", client => client.BaseAddress = new Uri("http://localhost/"))
    .AddHttpMessageHandler<HAMS.WebHost.Client.Services.BearerTokenHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("HAMS.Api"));
builder.Services.AddScoped<HAMS.WebHost.Client.Services.FileDownloader>();
builder.Services.AddScoped<HAMS.WebHost.Client.Portal.PortalReferenceDataCache>();

// Enums serialize/bind as their string name (e.g. "Monday", "Published") everywhere in the API,
// not their numeric value — applies globally so every enum (RecordStatus, DayOfWeek, etc.)
// benefits, not just whichever endpoint happens to hit it first.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Platform.* kernels — built once, reused by every module (see build plan §1.4).
builder.Services
    .AddPlatformCommon()
    .AddPlatformAudit(builder.Configuration)
    .AddPlatformAccess(builder.Configuration)
    .AddPlatformWorkflow()
    .AddPlatformDocuments()
    .AddPlatformNotifications(builder.Configuration);

// Business modules (see build plan §2's Module Boundaries table). This is the only place
// in the solution all 12 modules get wired together.
builder.Services
    .AddIdentityAccessModule(builder.Configuration)
    .AddOrgCurriculumModule(builder.Configuration)
    .AddPeopleEnrollmentModule(builder.Configuration)
    .AddTeachingTimetableModule(builder.Configuration)
    .AddLearningDeliveryModule(builder.Configuration)
    .AddAssessmentEvaluationModule(builder.Configuration)
    .AddInterventionModule(builder.Configuration)
    .AddAttendanceModule(builder.Configuration)
    .AddCommunicationPortalsModule(builder.Configuration)
    .AddReportingAnalyticsAuditModule(builder.Configuration)
    .AddAdminIntegrationModule()
    .AddFundraisingModule();

var app = builder.Build();

// Runs in EVERY environment, not just Development — this solo/no-dedicated-DBA deployment has no
// separate ops-run migration pipeline step, so a fresh Production database (and any future
// module's fresh schema) depends entirely on this running on startup, the same as it always has
// in Development. Only the seeder below (a hardcoded, publicly-known password) stays dev-only.
using (var scope = app.Services.CreateScope())
{
    var provider = scope.ServiceProvider;
    await provider.GetRequiredService<AuditDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<AccessDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<IdentityAccessDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<OrgDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<PeopleDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<TeachingTimetableDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<AttendanceDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<LearningDeliveryDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<AssessmentEvaluationDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<InterventionDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<ReportingAnalyticsAuditDbContext>().Database.MigrateAsync();
    await provider.GetRequiredService<CommunicationPortalsDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    await DevelopmentDataSeeder.SeedAsync(app.Services);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(HAMS.WebHost.Client._Imports).Assembly);

app.MapAccountEndpoints();

// Deploy-script health check (CD pipeline) — deliberately unauthenticated and does no DB round
// trip: it only needs to prove the process is up and Kestrel is accepting requests behind IIS,
// not that every dependency is healthy.
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();

// Business module API endpoints (versioned under /api/v1 from Phase 1 onward — see build
// plan §1.5 API-first decision). Empty minimal-API groups until each module's phase begins.
app.MapIdentityAccessModuleEndpoints()
    .MapOrgCurriculumModuleEndpoints()
    .MapPeopleEnrollmentModuleEndpoints()
    .MapTeachingTimetableModuleEndpoints()
    .MapLearningDeliveryModuleEndpoints()
    .MapAssessmentEvaluationModuleEndpoints()
    .MapInterventionModuleEndpoints()
    .MapAttendanceModuleEndpoints()
    .MapCommunicationPortalsModuleEndpoints()
    .MapReportingAnalyticsAuditModuleEndpoints()
    .MapAdminIntegrationModuleEndpoints()
    .MapFundraisingModuleEndpoints();

// Notifications is a Platform kernel, not a business module — no natural owning module to host
// this endpoint group, so it's mapped directly here (build plan Phase D).
app.MapNotificationAdminEndpoints();

app.Run();
