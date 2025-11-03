namespace Hydra.Api.Contracts.Bookings;

public record CreateBookingRequest(
    Guid VenueId,
    Guid CustomerId,
    DateTime StartUtc,
    DateTime EndUtc,
    int PartySize);

public record BookingDto(
    Guid Id,
    Guid VenueId,
    Guid CustomerId,
    DateTime StartUtc,
    DateTime EndUtc,
    int PartySize,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record BookingDecisionRequest(
    string Admin,      // who decided (identifier/email)
    string? Note = null);

public record CancelBookingRequest(
    string? CancelledBy = null,
    string? Reason = null
);

public record AvailabilityDto(
    Guid VenueId,
    DateOnly Date,
    int PartySize,
    bool IsAvailable,
    string Reason,
    List<TimeSlot> AvailableSlots);

public record TimeSlot(
    DateTime StartUtc,
    DateTime EndUtc);

