using HAMS.Platform.Notifications.Domain;

namespace HAMS.Platform.Notifications.Infrastructure;

/// <summary>Fixed reference data seeded via EF Core migrations — see <c>AttendanceSeedData</c> for the same pattern.</summary>
internal static class NotificationsSeedData
{
    public static readonly NotificationChannel[] NotificationChannels =
    [
        new() { Id = new Guid("00000000-0000-0000-0017-000000000001"), Code = NotificationChannelCodes.Sms, Name = "SMS", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0017-000000000002"), Code = NotificationChannelCodes.Email, Name = "Email", DisplayOrder = 2 },
    ];
}
