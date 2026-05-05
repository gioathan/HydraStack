using System.Threading.Channels;

namespace Hydra.Api.Services.Notifications;

public interface INotificationQueue
{
    void Enqueue(BookingNotification notification);
    IAsyncEnumerable<BookingNotification> ReadAllAsync(CancellationToken ct);
}
