using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hydra.Api.Services.Notifications;

public class NotificationWorker : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly ILogger<NotificationWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public NotificationWorker(
        INotificationQueue queue,
        ILogger<NotificationWorker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(notification, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled error processing {Type} notification for booking {BookingId}",
                    notification.Type, notification.BookingId);
            }
        }
    }

    private async Task ProcessAsync(BookingNotification n, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        await SendEmailAsync(scope, n, ct);
        await SendPushAsync(scope, n, ct);
    }

    private async Task SendEmailAsync(IServiceScope scope, BookingNotification n, CancellationToken ct)
    {
        var recipientEmail = GetRecipientEmail(n);
        if (string.IsNullOrEmpty(recipientEmail))
            return;

        var emailService = scope.ServiceProvider.GetService<IEmailService>();
        if (emailService is null)
        {
            _logger.LogDebug(
                "TODO: IEmailService is not registered — email skipped for {Type} (booking {BookingId})",
                n.Type, n.BookingId);
            return;
        }

        try
        {
            var (subject, htmlBody) = NotificationContent.GetEmailContent(n);
            await emailService.SendAsync(recipientEmail, subject, htmlBody, ct);
            _logger.LogInformation(
                "Email sent to {Email} for {Type} (booking {BookingId})",
                recipientEmail, n.Type, n.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to send email to {Email} for {Type} (booking {BookingId})",
                recipientEmail, n.Type, n.BookingId);
        }
    }

    private async Task SendPushAsync(IServiceScope scope, BookingNotification n, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(n.CustomerPushToken))
            return;

        var pushContent = NotificationContent.GetPushContent(n);
        if (pushContent is null)
            return;

        try
        {
            var expoPushService = scope.ServiceProvider.GetRequiredService<IExpoPushService>();
            var notification = new ExpoNotification(
                To: n.CustomerPushToken,
                Title: pushContent.Value.Title,
                Body: pushContent.Value.Body,
                Data: new Dictionary<string, string> { ["bookingId"] = n.BookingId.ToString() });

            var sent = await expoPushService.SendAsync(notification, ct);

            if (sent)
                _logger.LogInformation(
                    "Push notification sent for {Type} (booking {BookingId})",
                    n.Type, n.BookingId);
            else
                _logger.LogWarning(
                    "Expo rejected push for {Type} (booking {BookingId})",
                    n.Type, n.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to send push notification for {Type} (booking {BookingId})",
                n.Type, n.BookingId);
        }
    }

    private static string? GetRecipientEmail(BookingNotification n) => n.Type switch
    {
        NotificationType.BookingReceived => n.VenueAdminEmail,
        NotificationType.BookingCancelledByCustomer => n.VenueAdminEmail,
        NotificationType.BookingConfirmed => n.CustomerEmail,
        NotificationType.BookingDeclined => n.CustomerEmail,
        NotificationType.BookingCancelledByVenue => n.CustomerEmail,
        _ => null
    };
}
