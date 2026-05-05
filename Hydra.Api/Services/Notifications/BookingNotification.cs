namespace Hydra.Api.Services.Notifications;

public enum NotificationType
{
    // Sent to venue admin when customer creates a booking
    BookingReceived,
    // Sent to customer when venue confirms
    BookingConfirmed,
    // Sent to customer when venue declines
    BookingDeclined,
    // Sent to venue admin when customer cancels
    BookingCancelledByCustomer,
    // Sent to customer when venue cancels
    BookingCancelledByVenue
}

public record BookingNotification(
    NotificationType Type,
    // Venue admin details
    string VenueAdminEmail,
    string VenueName,
    // Customer details
    string CustomerEmail,
    string CustomerName,
    string? CustomerPushToken,
    // Booking details
    Guid BookingId,
    DateTime StartUtc,
    int PartySize,
    string? Note = null);
