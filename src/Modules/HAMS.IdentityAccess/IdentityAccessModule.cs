using System.Text;
using HAMS.IdentityAccess.Application.Auth;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.IdentityAccess.Domain;
using HAMS.IdentityAccess.Endpoints;
using HAMS.IdentityAccess.Infrastructure;
using HAMS.Platform.Audit.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace HAMS.IdentityAccess;

/// <summary>
/// Module registration entry point for the IdentityAccess module (see the build plan's Module Boundaries
/// table). `HAMS.WebHost`'s `Program.cs` is the only place this gets called — no other module
/// may reference `HAMS.IdentityAccess`'s internals directly, only its public Application contracts (once
/// there are any) and this registration surface.
/// </summary>
public static class IdentityAccessModule
{
    /// <summary>
    /// Registers ASP.NET Core Identity (staff accounts, password + built-in TOTP MFA), JWT issuance
    /// and validation, and the "identity" schema's <see cref="IdentityAccessDbContext"/>.
    /// </summary>
    public static IServiceCollection AddIdentityAccessModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddDbContext<IdentityAccessDbContext>((sp, options) => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity"))
            .AddInterceptors(sp.GetRequiredService<SaveChangesGuardInterceptor>()));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<IdentityAccessDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ITokenIssuer, TokenIssuer>();
        services.AddScoped<IStaffAuthenticationService, StaffAuthenticationService>();
        services.AddScoped<IStaffAccountService, StaffAccountService>();
        services.AddScoped<ISetupService, SetupService>();
        services.AddScoped<IGuardianAuthenticationService, GuardianAuthenticationService>();
        services.AddScoped<IStudentAuthenticationService, StudentAuthenticationService>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Missing required 'Jwt' configuration section.");

        // Two real schemes (JWT for every existing API endpoint, unchanged; Cookie for Phase 12's
        // Blazor Server admin UI) selected by a PolicyScheme based on whether the request actually
        // carries a bearer token — NOT a scheme-restricted [Authorize] attribute, which Blazor's own
        // component-level authorization explicitly rejects ("Authentication schemes cannot be
        // specified for components"), and NOT a path-based selector either: several Phase 12
        // endpoints (regulatory-report downloads) deliberately live under the same /api/v1/...
        // prefix as every other endpoint (API-first, build plan §1.5) but are reached via a plain
        // browser link/download, which never carries an Authorization header — a path-based
        // selector would incorrectly route those to JWT and 401 a logged-in browser. Selecting on
        // header presence instead handles both correctly: a real Bearer-token API client (mobile,
        // curl, a future MAUI client) always sends the header and gets JWT; any browser-originated
        // request (a Razor page, or a plain <a href> hitting an /api/... download endpoint) never
        // does and gets Cookie — whose own challenge redirects to login, exactly what a browser
        // navigation wants instead of a bare 401. Every existing [Authorize]/RequireAuthorization()
        // call (11 phases' worth) and every bare [Authorize] on a Razor page both keep working
        // unmodified — each just resolves against whichever real scheme this selector forwards to.
        const string PolicySchemeName = "HAMS.Smart";

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = PolicySchemeName;
                options.DefaultChallengeScheme = PolicySchemeName;
            })
            .AddPolicyScheme(PolicySchemeName, "JWT for bearer-token requests, Cookie for everything else", options =>
            {
                options.ForwardDefaultSelector = context =>
                    context.Request.Headers.ContainsKey("Authorization")
                        ? JwtBearerDefaults.AuthenticationScheme
                        : CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/account/login";
                options.Cookie.Name = "HAMS.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

        // Authorization services (IAuthorizationService, policies) are registered by
        // AddPlatformAccess — always called before this in Program.cs.

        return services;
    }

    /// <summary>Maps this module's minimal-API endpoint groups.</summary>
    public static IEndpointRouteBuilder MapIdentityAccessModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAuthEndpoints();
        endpoints.MapIdentityAdminEndpoints();
        endpoints.MapStaffAccountEndpoints();
        endpoints.MapSetupEndpoints();
        endpoints.MapGuardianAuthEndpoints();
        endpoints.MapStudentAuthEndpoints();
        return endpoints;
    }
}
