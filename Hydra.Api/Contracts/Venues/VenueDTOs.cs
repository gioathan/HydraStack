namespace Hydra.Api.Contracts.Venues;

public record CreateVenueRequest(
    string Name,
    string Address,
    int Capacity);

public record UpdateVenueRequest(
    string Name,
    string Address,
    int Capacity);

public record VenueDto(
    Guid Id,
    string Name,
    string Address,
    int Capacity);
