using Hydra.Api.Data;
using Hydra.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Services.Notifications;

public class RatingNotificationWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RatingNotificationWorker> _logger;

    public RatingNotificationWorker(IServiceScopeFactory scopeFactory, ILogger<RatingNotificationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in RatingNotificationWorker");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pushService = scope.ServiceProvider.GetRequiredService<IExpoPushService>();

        var now = DateTime.UtcNow;

        var bookings = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Where(b =>
                b.Status == BookingStatus.Confirmed &&
                b.EndUtc <= now &&
                b.RatingNotificationSentAt == null &&
                b.Customer.PushToken != null &&
                !db.VenueRatings.Any(r => r.CustomerId == b.CustomerId && r.VenueId == b.VenueId))
            .ToListAsync(ct);

        if (bookings.Count == 0)
            return;

        var notifications = bookings
            .Where(b => b.Customer.PushToken is not null)
            .Select(b => new ExpoNotification(
                To: b.Customer.PushToken!,
                Title: "How was your visit?",
                Body: "Rate your experience at the venue.",
                Data: new Dictionary<string, string>
                {
                    ["venueId"] = b.VenueId.ToString(),
                    ["bookingId"] = b.Id.ToString(),
                    ["type"] = "rating_prompt"
                }))
            .ToList();

        if (notifications.Count > 0)
            await pushService.SendBatchAsync(notifications, ct);

        // Mark as sent regardless of push outcome so we don't spam
        var ids = bookings.Select(b => b.Id).ToList();
        await db.Bookings
            .Where(b => ids.Contains(b.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.RatingNotificationSentAt, now), ct);

        _logger.LogInformation("Sent rating notifications for {Count} bookings", bookings.Count);
    }
}
