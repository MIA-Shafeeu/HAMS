using HAMS.WebHost.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The WASM runtime is a separate DI container from the server (build plan Phase C1) — anything a
// guardian/student page injects has to be registered here, independently of HAMS.WebHost's Program.cs.
builder.Services.AddMudServices();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<PortalAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<PortalAuthenticationStateProvider>());
builder.Services.AddScoped<BearerTokenHandler>();

// Handler-free client used only for the refresh call itself — BearerTokenHandler resolves this via
// IHttpClientFactory, so it can't be attached to the same client it lives on without recursing.
builder.Services.AddHttpClient("HAMS.Api.Refresh", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddHttpClient("HAMS.Api", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("HAMS.Api"));

builder.Services.AddScoped<FileDownloader>();
builder.Services.AddScoped<HAMS.WebHost.Client.Portal.PortalReferenceDataCache>();

await builder.Build().RunAsync();
