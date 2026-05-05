using System.Threading.Channels;

namespace Hydra.Api.Services.Notifications;

public class NotificationQueue : INotificationQueue
{
    private readonly Channel<BookingNotification> _channel =
        Channel.CreateUnbounded<BookingNotification>();

    public void Enqueue(BookingNotification notification) =>
        _channel.Writer.TryWrite(notification);

    public IAsyncEnumerable<BookingNotification> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
