namespace Hydra.Api.Contracts.VenueTypes;

public record VenueTypeDto(
    Guid Id,
    string Name,
    string? Description,
    int DisplayOrder);

public record CreateVenueTypeRequest(
    string Name,
    string? Description = null,
    int DisplayOrder = 0);

public record UpdateVenueTypeRequest(
    string Name,
    string? Description,
    int DisplayOrder);