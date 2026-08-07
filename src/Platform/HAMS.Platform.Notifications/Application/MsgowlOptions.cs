namespace HAMS.Platform.Notifications.Application;

/// <summary>
/// Configuration for the real Maldivian bulk-SMS gateway adapter (build plan §5's "secondary
/// provider," now available — Message Owl, <c>https://rest.msgowl.com</c>). <see cref="Enabled"/>
/// defaults to <see langword="false"/> even when credentials are present: flipping it on is a
/// deliberate, explicit choice, since enabling it means every queued SMS notification (attendance
/// absences, etc.) becomes a real message to a real phone number instead of a log line — this must
/// never happen silently just because a config section exists.
/// </summary>
public sealed class MsgowlOptions
{
    public const string SectionName = "Msgowl";

    public bool Enabled { get; set; }

    public string? ApiKey { get; set; }

    /// <summary>One of the account's own approved sender IDs (see the Sender IDs endpoint) — never a free-typed value.</summary>
    public string? SenderId { get; set; }

    public string BaseUrl { get; set; } = "https://rest.msgowl.com";
}
