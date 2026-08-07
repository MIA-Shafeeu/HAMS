using Microsoft.Extensions.Logging;

namespace HAMS.Platform.Notifications.Application;

/// <summary>
/// The log-only dev adapter named explicitly in the build plan §5 ("secondary provider + log-only
/// dev adapter"). Registered until a real Dhiraagu/Ooredoo bulk-SMS account and credentials exist —
/// swapping in a real <see cref="ISmsSender"/> implementation is the only change needed then.
/// </summary>
internal sealed class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[dev SMS] To {PhoneNumber}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}
