using HAMS.Platform.Common.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.Platform.Common;

/// <summary>
/// Registers the handful of process-wide services every other kernel/module relies on
/// (currently just <see cref="IClock"/>). Called first, before any other <c>AddPlatformX()</c>.
/// </summary>
public static class PlatformCommonExtensions
{
    public static IServiceCollection AddPlatformCommon(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}
