using System.Globalization;

namespace HAMS.Mobile.Services;

/// <summary>
/// Device-secure-storage-backed token store — the MAUI equivalent of the WASM portal's
/// <c>localStorage</c>-backed <c>TokenStorage</c> (build plan Phase 14). Uses <see cref="SecureStorage"/>
/// (platform Keystore/Keychain-backed, per build plan §6's own MAUI storage guidance) rather than
/// plain <see cref="Preferences"/>, since these are auth tokens, not app settings.
/// </summary>
public sealed class TokenStorage
{
    private const string AccessTokenKey = "hams.mobile.accessToken";
    private const string RefreshTokenKey = "hams.mobile.refreshToken";
    private const string ExpiresAtKey = "hams.mobile.accessTokenExpiresAtUtc";

    public async Task SaveAsync(string accessToken, string refreshToken, DateTimeOffset accessTokenExpiresAtUtc)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
        await SecureStorage.Default.SetAsync(ExpiresAtKey, accessTokenExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture));
    }

    public Task<string?> GetAccessTokenAsync() => SecureStorage.Default.GetAsync(AccessTokenKey);

    public Task<string?> GetRefreshTokenAsync() => SecureStorage.Default.GetAsync(RefreshTokenKey);

    public async Task<DateTimeOffset?> GetAccessTokenExpiresAtUtcAsync()
    {
        var raw = await SecureStorage.Default.GetAsync(ExpiresAtKey);
        return raw is not null
            && DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value)
            ? value
            : null;
    }

    public void Clear() => SecureStorage.Default.RemoveAll();
}
