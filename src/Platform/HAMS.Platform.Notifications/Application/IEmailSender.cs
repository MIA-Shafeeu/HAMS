namespace HAMS.Platform.Notifications.Application;

/// <summary>Adapter over an SMTP/email provider. Only <see cref="LoggingEmailSender"/> exists today — see <see cref="ISmsSender"/>'s remarks.</summary>
public interface IEmailSender
{
    Task SendAsync(string emailAddress, string subject, string body, CancellationToken cancellationToken = default);
}
