namespace HAMS.Platform.Notifications.Application;

/// <summary>
/// Adapter over a Maldivian carrier bulk-SMS gateway (build plan §5: Dhiraagu/Ooredoo — configurable
/// per INTG-FR-004). Only <see cref="LoggingSmsSender"/> exists today; a real carrier adapter is a
/// separate, later concern once carrier credentials/an account are actually available.
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
