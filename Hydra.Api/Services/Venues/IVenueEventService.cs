using Hydra.Api.Contracts.Venues;

namespace Hydra.Api.Services.Venues;

public interface IVenueEventService
{
    Task<IReadOnlyList<VenueEventDto>> GetEventsAsync(Guid venueId, bool includePast, CancellationToken ct = default);
    Task<VenueEventDto?> GetEventByIdAsync(Guid venueId, Guid eventId, CancellationToken ct = default);
    Task<(VenueEventDto? Result, string? Error)> CreateEventAsync(Guid venueId, CreateVenueEventRequest request, CancellationToken ct = default);
    Task<(VenueEventDto? Result, string? Error)> UpdateEventAsync(Guid venueId, Guid eventId, UpdateVenueEventRequest request, CancellationToken ct = default);
    Task<bool> DeleteEventAsync(Guid venueId, Guid eventId, CancellationToken ct = default);
    Task<VenueEventDto?> CloseEventAsync(Guid venueId, Guid eventId, CancellationToken ct = default);
    Task<VenueEventDto?> AddEventPhotoAsync(Guid venueId, Guid eventId, AddEventPhotoRequest request, CancellationToken ct = default);
    Task<bool> DeleteEventPhotoAsync(Guid venueId, Guid eventId, Guid photoId, CancellationToken ct = default);
}
