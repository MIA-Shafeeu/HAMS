using Microsoft.Extensions.Logging;

namespace HAMS.Platform.Notifications.Application;

/// <summary>The log-only dev adapter for email — see <see cref="LoggingSmsSender"/>'s remarks.</summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string emailAddress, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[dev Email] To {EmailAddress}, Subject: {Subject}: {Body}", emailAddress, subject, body);
        return Task.CompletedTask;
    }
}
