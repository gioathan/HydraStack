using System.Globalization;

namespace Hydra.Api.Services.Notifications;

public static class NotificationContent
{
    public static (string Subject, string HtmlBody) GetEmailContent(BookingNotification n)
    {
        var date = n.StartUtc.ToString("dddd, dd MMM", CultureInfo.InvariantCulture);
        var time = n.StartUtc.ToString("HH:mm", CultureInfo.InvariantCulture);

        return n.Type switch
        {
            NotificationType.BookingReceived => (
                $"New Booking Request — {n.VenueName}",
                $"{n.CustomerName} has requested a table for {n.PartySize} on {date} at {time} UTC."),

            NotificationType.BookingConfirmed => (
                $"Your booking at {n.VenueName} is confirmed!",
                $"Great news! Your table for {n.PartySize} on {date} at {time} UTC has been confirmed."),

            NotificationType.BookingDeclined => (
                $"Booking update from {n.VenueName}",
                $"Unfortunately your booking request for {date} at {time} UTC could not be accommodated."),

            NotificationType.BookingCancelledByCustomer => (
                $"Booking Cancelled — {n.VenueName}",
                $"{n.CustomerName}'s booking for {n.PartySize} on {date} at {time} UTC has been cancelled."),

            NotificationType.BookingCancelledByVenue => (
                "Your booking has been cancelled",
                $"Your booking at {n.VenueName} for {date} at {time} UTC has been cancelled by the venue."),

            _ => ("Booking Update", "There has been an update to your booking.")
        };
    }

    public static (string Title, string Body)? GetPushContent(BookingNotification n) => n.Type switch
    {
        NotificationType.BookingConfirmed => (
            "Booking Confirmed!",
            $"Your table at {n.VenueName} for {n.StartUtc:HH:mm} is confirmed."),

        NotificationType.BookingDeclined => (
            "Booking Update",
            $"{n.VenueName} could not accommodate your request."),

        NotificationType.BookingCancelledByVenue => (
            "Booking Cancelled",
            $"Your booking at {n.VenueName} has been cancelled."),

        // BookingReceived and BookingCancelledByCustomer go to venue admin — no push token in this system
        _ => null
    };
}
