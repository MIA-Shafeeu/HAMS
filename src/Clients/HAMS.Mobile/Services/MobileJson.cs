using System.Text.Json;

namespace HAMS.Mobile.Services;

/// <summary>
/// One shared, explicit <see cref="JsonSerializerOptions"/> for every mobile HTTP call — same
/// reasoning as the WASM portal's own <c>PortalJson.Options</c> (build plan Phase C2 lesson):
/// the server's minimal-API endpoints emit camelCase, so don't rely on whichever
/// <c>System.Net.Http.Json</c> overload's implicit default happens to apply.
/// </summary>
public static class MobileJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
