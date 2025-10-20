namespace Hydra.Api.Contracts.Bookings;

public record CreateBookingRequest(
    Guid VenueId,
    Guid CustomerId,
    DateTime StartUtc,
    DateTime EndUtc,
    int PartySize,
    string? CustomerNote);

public record BookingDecisionRequest(
    string? Admin,   
    string? Note); 

public record BookingDto(
    Guid Id,
    Guid VenueId,
    Guid CustomerId,
    DateTime StartUtc,
    DateTime EndUtc,
    int PartySize,
    string Status,
    DateTime RequestedAtUtc,
    DateTime? DecidedAtUtc,
    string? CustomerNote,
    string? AdminNote);
