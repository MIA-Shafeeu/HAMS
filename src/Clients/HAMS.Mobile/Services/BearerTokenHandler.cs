using System.Net.Http.Headers;
using System.Net.Http.Json;
using HAMS.SharedContracts.Auth;

namespace HAMS.Mobile.Services;

/// <summary>
/// Attaches <c>Authorization: Bearer</c> to every outgoing API call, proactively refreshing via
/// <c>/api/v1/auth/refresh</c> first if the stored access token is at or near expiry — the exact
/// same design as the WASM portal's own <c>BearerTokenHandler</c> (build plan Phase C1), since both
/// clients converge on the same bearer+opaque-refresh-token scheme. The refresh call goes through a
/// separate handler-free named client to avoid this handler re-entering itself.
/// </summary>
public sealed class BearerTokenHandler(TokenStorage tokenStorage, IHttpClientFactory httpClientFactory) : DelegatingHandler
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
            tokenStorage.Clear();
            return null;
        }

        var client = httpClientFactory.CreateClient("HAMS.Api.Refresh");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("api/v1/auth/refresh", new RefreshRequestDto(refreshToken), MobileJson.Options, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Offline/unreachable — keep the (possibly still-valid) stored token rather than signing
            // the user out just because the refresh round-trip couldn't complete.
            return await tokenStorage.GetAccessTokenAsync();
        }

        if (!response.IsSuccessStatusCode)
        {
            tokenStorage.Clear();
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(MobileJson.Options, cancellationToken);
        if (result is null || !result.Succeeded || result.AccessToken is null || result.RefreshToken is null || result.AccessTokenExpiresAtUtc is null)
        {
            tokenStorage.Clear();
            return null;
        }

        await tokenStorage.SaveAsync(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAtUtc.Value);
        return result.AccessToken;
    }
}
