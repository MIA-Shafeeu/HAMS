using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace HAMS.Platform.Notifications.Application;

/// <summary>
/// Real adapter over Message Owl's bulk-SMS REST API (build plan §5) — the "secondary provider"
/// alongside <see cref="LoggingSmsSender"/>. Only sends when <see cref="MsgowlOptions.Enabled"/> is
/// explicitly set (see its remarks); which of the two implementations <see cref="ISmsSender"/>
/// resolves to is decided once, in <c>PlatformNotificationsExtensions</c>.
/// </summary>
internal sealed class MsgowlSmsSender(HttpClient httpClient, IOptions<MsgowlOptions> options) : ISmsSender
{
    public async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "messages")
        {
            Content = JsonContent.Create(new SendMessageRequest(NormalizePhoneNumber(phoneNumber), options.Value.SenderId ?? string.Empty, message)),
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"AccessKey {options.Value.ApiKey}");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Msgowl SMS send failed ({(int)response.StatusCode}): {body}");
        }
    }

    /// <summary>Message Owl expects a bare digit string (e.g. <c>9609999999</c>) — this codebase stores numbers as <c>+960...</c>.</summary>
    private static string NormalizePhoneNumber(string phoneNumber) => new(phoneNumber.Where(char.IsAsciiDigit).ToArray());

    private sealed record SendMessageRequest(
        [property: JsonPropertyName("recipients")] string Recipients,
        [property: JsonPropertyName("sender_id")] string SenderId,
        [property: JsonPropertyName("body")] string Body);
}
