namespace Hydra.Api.Contracts.Ratings;

public record SubmitRatingRequest(Guid BookingId, decimal Value);

public record PendingRatingDto(
    Guid VenueId,
    string VenueName,
    Guid BookingId,
    DateTime BookingEndUtc);
