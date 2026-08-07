using System.Globalization;
using Microsoft.JSInterop;

namespace HAMS.WebHost.Client.Services;

/// <summary>
/// Browser <c>localStorage</c>-backed token store for the guardian/student portal. There is no
/// server-side cookie/session for these principal types (build plan Phase C: "true WebAssembly... a
/// guardian on a remote island needs a UI that survives a dropped connection") — the access/refresh
/// tokens issued by <c>/api/v1/auth/guardian/otp/verify</c> and <c>/api/v1/auth/student/login</c> live
/// only in the browser. Calls <c>localStorage.*</c> directly via <see cref="IJSRuntime"/> rather than a
/// dedicated JS module — there's nothing here beyond three key/value pairs.
/// </summary>
public sealed class TokenStorage(IJSRuntime jsRuntime)
{
    private const string AccessTokenKey = "hams.portal.accessToken";
    private const string RefreshTokenKey = "hams.portal.refreshToken";
    private const string ExpiresAtKey = "hams.portal.accessTokenExpiresAtUtc";

    public async Task SaveAsync(string accessToken, string refreshToken, DateTimeOffset accessTokenExpiresAtUtc)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", ExpiresAtKey, accessTokenExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture));
    }

    public async Task<string?> GetAccessTokenAsync() =>
        await jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);

    public async Task<string?> GetRefreshTokenAsync() =>
        await jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);

    public async Task<DateTimeOffset?> GetAccessTokenExpiresAtUtcAsync()
    {
        var raw = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", ExpiresAtKey);
        return raw is not null
            && DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value)
            ? value
            : null;
    }

    public async Task ClearAsync()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", ExpiresAtKey);
    }
}
