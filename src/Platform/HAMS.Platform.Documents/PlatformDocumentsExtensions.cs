using Microsoft.Extensions.DependencyInjection;

namespace HAMS.Platform.Documents;

/// <summary>
/// Registration entry point for the Documents platform kernel. Every business module and
/// `HAMS.WebHost` may depend on this; per the plan's kernel design, this is reused rather than
/// re-implemented per-module.
/// </summary>
public static class PlatformDocumentsExtensions
{
    /// <summary>Registers the Documents kernel's services. Fleshed out as its build phase begins.</summary>
    public static IServiceCollection AddPlatformDocuments(this IServiceCollection services)
    {
        return services;
    }
}
