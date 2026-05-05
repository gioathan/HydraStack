namespace Hydra.Api.Services.Notifications;

public interface IExpoPushService
{
    /// Sends a push notification to an Expo push token.
    /// Returns true if accepted by Expo, false otherwise.
    /// Never throws — fails gracefully and logs.
    Task<bool> SendAsync(ExpoNotification notification,
        CancellationToken ct = default);

    /// Sends multiple notifications in a single batch (max 100 per Expo docs).
    Task<bool> SendBatchAsync(IEnumerable<ExpoNotification> notifications,
        CancellationToken ct = default);
}
