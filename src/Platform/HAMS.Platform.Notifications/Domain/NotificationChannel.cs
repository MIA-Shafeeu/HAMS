namespace HAMS.Platform.Notifications.Domain;

/// <summary>
/// Configurable lookup (build plan §1.6 explicitly names "NotificationChannel" as an example), not
/// an enum — a school-specific deployment may add a channel (e.g. a local messaging app) without a
/// recompile. Push is deferred until Phase 14 (mobile), so only SMS/Email are seeded today.
/// </summary>
public sealed class NotificationChannel
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class NotificationChannelCodes
{
    public const string Sms = "SMS";
    public const string Email = "EMAIL";
}
