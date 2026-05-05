namespace Hydra.Api.Services.Notifications;

public record ExpoNotification(
    string To,
    string Title,
    string Body,
    Dictionary<string, string>? Data = null);

public record ExpoPushResponse(
    string Status,
    string? Message,
    string? Details);
