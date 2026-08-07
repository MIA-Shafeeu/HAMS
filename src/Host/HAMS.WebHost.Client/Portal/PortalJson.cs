using System.Text.Json;

namespace HAMS.WebHost.Client.Portal;

/// <summary>
/// One shared, explicit <see cref="JsonSerializerOptions"/> for every portal HTTP call — the server's
/// minimal-API endpoints emit camelCase property names (ASP.NET Core's own default), so client-side
/// (de)serialization against PascalCase C# record properties needs case-insensitive, camelCase-aware
/// options. Passed explicitly everywhere rather than relied on as an implicit default, so this
/// doesn't depend on exactly which <c>System.Net.Http.Json</c> overload happens to be called.
/// </summary>
public static class PortalJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
