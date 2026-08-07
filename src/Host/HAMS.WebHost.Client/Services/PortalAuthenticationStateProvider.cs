using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace HAMS.WebHost.Client.Services;

/// <summary>
/// The WASM-side counterpart to the server's cookie-based auth for staff pages — guardian/student
/// pages run in a genuinely separate DI container/process (the browser), so they need their own
/// <see cref="AuthenticationStateProvider"/> reading from <see cref="TokenStorage"/> instead of an
/// <c>HttpContext</c> cookie. No third-party OIDC package is used (see build plan Phase C1 note): the
/// server's custom bearer+opaque-refresh-token scheme doesn't fit one, so claims are decoded from the
/// JWT payload by hand.
/// </summary>
public sealed class PortalAuthenticationStateProvider(TokenStorage tokenStorage) : AuthenticationStateProvider
{
    private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var accessToken = await tokenStorage.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            return AnonymousState;
        }

        var expiresAtUtc = await tokenStorage.GetAccessTokenExpiresAtUtcAsync();
        if (expiresAtUtc is null || expiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return AnonymousState;
        }

        var identity = new ClaimsIdentity(ParseClaimsFromJwt(accessToken), authenticationType: "hams-portal");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    /// <summary>Call after login/logout/token-refresh so every <c>&lt;AuthorizeView&gt;</c> re-renders against the new token state.</summary>
    public void NotifyAuthenticationStateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    /// <summary>
    /// Guardian/student pages check this themselves in <c>OnInitializedAsync</c> rather than
    /// declaring <c>[Authorize]</c> — an <c>AuthorizeRouteView</c> gate is evaluated against
    /// whichever <see cref="AuthenticationStateProvider"/> is active for the CURRENT render pass,
    /// which for the initial (server-side, pre-WASM-boot) pass is always the server's cookie-based
    /// provider — it would see every guardian/student as anonymous and reject them permanently,
    /// since they never hold a staff cookie at all.
    /// </summary>
    public async Task<bool> HasClaimAsync(string claimType, string value)
    {
        var state = await GetAuthenticationStateAsync();
        return state.User.Claims.Any(c => c.Type == claimType && c.Value == value);
    }

    private static List<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var json = Encoding.UTF8.GetString(Base64UrlDecode(payload));
        var root = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];

        var claims = new List<Claim>();
        foreach (var (claimType, value) in root)
        {
            if (value.ValueKind == JsonValueKind.Array)
            {
                claims.AddRange(value.EnumerateArray().Select(item => new Claim(claimType, item.ToString())));
            }
            else
            {
                claims.Add(new Claim(claimType, value.ToString()));
            }
        }

        return claims;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(padded);
    }
}
