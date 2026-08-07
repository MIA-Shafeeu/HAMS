using HAMS.Mobile.Pages;
using HAMS.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace HAMS.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Auth infrastructure (build plan Phase 14) — mirrors the WASM portal's own Program.cs
		// registrations (build plan Phase C1): TokenStorage/BearerTokenHandler/a refresh-only named
		// client to avoid the handler re-entering itself.
		builder.Services.AddSingleton<TokenStorage>();
		builder.Services.AddTransient<BearerTokenHandler>();

		builder.Services.AddHttpClient("HAMS.Api.Refresh", client => client.BaseAddress = new Uri(ApiConfig.BaseUrl));

		builder.Services.AddHttpClient("HAMS.Api", client => client.BaseAddress = new Uri(ApiConfig.BaseUrl))
			.AddHttpMessageHandler<BearerTokenHandler>();

		builder.Services.AddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("HAMS.Api"));
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<MobileApiService>();

		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<MfaPage>();
		builder.Services.AddTransient<TimetablePage>();
		builder.Services.AddTransient<AttendancePage>();

		return builder.Build();
	}
}
