namespace Hydra.Api.Contracts.Venues;

public record CreateVenueRequest(
    string Name,
    string Address,
    int Capacity,
    Guid VenueTypeId,
    Guid UserId,
    string? GooglePlaceId = null);

public record UpdateVenueRequest(
    string Name,
    string Address,
    int Capacity,
    Guid VenueTypeId,
    string? GooglePlaceId = null);

public record VenueDto(
    Guid Id,
    string Name,
    string Address,
    int Capacity,
    Guid UserId,
    Guid VenueTypeId,
    string? GooglePlaceId,
    string? PhotoUrl);
