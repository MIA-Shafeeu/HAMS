using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HAMS.SharedContracts.Auth;

namespace HAMS.Mobile.Services;

/// <summary>
/// Staff login/MFA/logout against the exact same <c>/api/v1/auth/staff/*</c> endpoints the web
/// login form already uses (build plan Phase 14 — "mobile is a client of the proven API, not new
/// business logic"). Mirrors the WASM portal's own auth flow (build plan Phase C1) but stores
/// tokens via <see cref="TokenStorage"/> (device secure storage) instead of browser localStorage.
/// </summary>
public sealed class AuthService(HttpClient http, TokenStorage tokenStorage)
{
    public async Task<AuthResultDto> LoginAsync(string usernameOrEmail, string password)
    {
        var response = await http.PostAsJsonAsync("api/v1/auth/staff/login", new StaffLoginDto(usernameOrEmail, password, DeviceLabel()), MobileJson.Options);
        return await HandleAuthResponseAsync(response);
    }

    public async Task<AuthResultDto> VerifyMfaAsync(string mfaToken, string code)
    {
        var response = await http.PostAsJsonAsync("api/v1/auth/staff/login/mfa", new StaffMfaVerifyDto(mfaToken, code, DeviceLabel()), MobileJson.Options);
        return await HandleAuthResponseAsync(response);
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await tokenStorage.GetRefreshTokenAsync();
        if (!string.IsNullOrEmpty(refreshToken))
        {
            try
            {
                await http.PostAsJsonAsync("api/v1/auth/logout", new RefreshRequestDto(refreshToken), MobileJson.Options);
            }
            catch (HttpRequestException)
            {
                // Best-effort server-side revocation — clear the local session regardless.
            }
        }

        tokenStorage.Clear();
    }

    /// <summary>
    /// A refresh token existing is enough to consider the user "logged in" — an expired access
    /// token gets silently refreshed by <see cref="BearerTokenHandler"/> on the next real API call,
    /// the same proactive-refresh design the WASM portal uses.
    /// </summary>
    public async Task<bool> IsLoggedInAsync()
    {
        var refreshToken = await tokenStorage.GetRefreshTokenAsync();
        return !string.IsNullOrEmpty(refreshToken);
    }

    public async Task<Guid?> GetPersonIdAsync()
    {
        var token = await tokenStorage.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var claims = ParseClaimsFromJwt(token);
        return claims.TryGetValue(HamsClaimTypes.PersonId, out var value) && Guid.TryParse(value, out var id) ? id : null;
    }

    private async Task<AuthResultDto> HandleAuthResponseAsync(HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(MobileJson.Options)
            ?? new AuthResultDto { Succeeded = false, Error = "Unexpected response from the server." };

        if (result is { Succeeded: true, AccessToken: not null, RefreshToken: not null, AccessTokenExpiresAtUtc: not null })
        {
            await tokenStorage.SaveAsync(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAtUtc.Value);
        }

        return result;
    }

    private static string DeviceLabel() => $"{DeviceInfo.Platform} {DeviceInfo.Model}";

    private static Dictionary<string, string> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var json = Encoding.UTF8.GetString(Base64UrlDecode(payload));
        var root = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];
        return root.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(padded);
    }
}
