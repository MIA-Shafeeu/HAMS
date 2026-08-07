using System.Net.Http.Headers;
using System.Net.Http.Json;
using HAMS.SharedContracts.Auth;
using HAMS.WebHost.Client.Portal;

namespace HAMS.WebHost.Client.Services;

/// <summary>
/// Attaches <c>Authorization: Bearer</c> to every outgoing portal API call, proactively refreshing
/// via <c>/api/v1/auth/refresh</c> first if the stored access token is at or near expiry. Proactive
/// (checked before the request) rather than reactive (retry-on-401) — simpler, and avoids a doubled
/// request on every near-expiry call. The refresh call itself goes through the separate
/// "HAMS.Api.Refresh" named client (no handler attached) to avoid this handler re-entering itself.
/// </summary>
public sealed class BearerTokenHandler(
    TokenStorage tokenStorage,
    IHttpClientFactory httpClientFactory,
    PortalAuthenticationStateProvider authStateProvider)
    : DelegatingHandler
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(30);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await tokenStorage.GetAccessTokenAsync();
        var expiresAtUtc = await tokenStorage.GetAccessTokenExpiresAtUtcAsync();

        if (!string.IsNullOrEmpty(accessToken) && (expiresAtUtc is null || expiresAtUtc <= DateTimeOffset.UtcNow.Add(RefreshSkew)))
        {
            accessToken = await TryRefreshAsync(cancellationToken);
        }

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> TryRefreshAsync(CancellationToken cancellationToken)
    {
        var refreshToken = await tokenStorage.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(refreshToken))
        {
            await tokenStorage.ClearAsync();
            return null;
        }

        var client = httpClientFactory.CreateClient("HAMS.Api.Refresh");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequestDto(refreshToken), PortalJson.Options, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Offline/unreachable — keep the (possibly still-valid) stored token rather than signing
            // the user out just because the refresh round-trip couldn't complete.
            return await tokenStorage.GetAccessTokenAsync();
        }

        if (!response.IsSuccessStatusCode)
        {
            await tokenStorage.ClearAsync();
            authStateProvider.NotifyAuthenticationStateChanged();
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(PortalJson.Options, cancellationToken);
        if (result is null || !result.Succeeded || result.AccessToken is null || result.RefreshToken is null || result.AccessTokenExpiresAtUtc is null)
        {
            await tokenStorage.ClearAsync();
            authStateProvider.NotifyAuthenticationStateChanged();
            return null;
        }

        await tokenStorage.SaveAsync(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAtUtc.Value);
        authStateProvider.NotifyAuthenticationStateChanged();
        return result.AccessToken;
    }
}
