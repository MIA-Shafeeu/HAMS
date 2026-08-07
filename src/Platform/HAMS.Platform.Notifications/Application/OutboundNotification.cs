namespace HAMS.Platform.Notifications.Application;

/// <summary>One notification a source module wants queued — <paramref name="ChannelCode"/> is a <c>NotificationChannelCodes</c> constant, resolved to the real lookup row at write time.</summary>
public sealed record OutboundNotification(string ChannelCode, string Recipient, string? Subject, string Body);
