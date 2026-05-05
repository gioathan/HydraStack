using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hydra.Api.Services.Notifications;

public class ExpoPushService : IExpoPushService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly ILogger<ExpoPushService> _logger;

    public ExpoPushService(IHttpClientFactory httpClientFactory, ILogger<ExpoPushService> logger)
    {
        _http = httpClientFactory.CreateClient("Expo");
        _logger = logger;
    }

    public Task<bool> SendAsync(ExpoNotification notification, CancellationToken ct = default) =>
        SendBatchAsync(new[] { notification }, ct);

    public async Task<bool> SendBatchAsync(IEnumerable<ExpoNotification> notifications, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(notifications, SerializerOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _http.PostAsync("--/api/v2/push/send", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Expo push request failed with HTTP {Status}", (int)response.StatusCode);
                return false;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var ticket in data.EnumerateArray())
                {
                    if (ticket.TryGetProperty("status", out var statusEl) &&
                        statusEl.GetString() == "error")
                    {
                        var message = ticket.TryGetProperty("message", out var msgEl)
                            ? msgEl.GetString() : null;
                        var details = ticket.TryGetProperty("details", out var detailsEl)
                            ? detailsEl.GetRawText() : null;
                        _logger.LogWarning(
                            "Expo ticket error: {Message} — details: {Details}", message, details);
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deliver batch to Expo push service");
            return false;
        }
    }
}
