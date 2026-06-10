using Hydra.Api.Models;

namespace Hydra.Api.Repositories.VenueEvents;

public interface IVenueEventRepository
{
    Task<List<VenueEvent>> GetByVenueIdAsync(Guid venueId, bool includePast, CancellationToken ct = default);
    Task<(List<VenueEvent> Items, int Total)> GetUpcomingPagedAsync(int skip, int take, string? location, CancellationToken ct = default);
    Task<VenueEvent?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasActiveEventOnDayAsync(Guid venueId, DateTime date, Guid? excludeId, CancellationToken ct = default);
    Task<VenueEvent> AddAsync(VenueEvent ev, CancellationToken ct = default);
    Task UpdateAsync(VenueEvent ev, CancellationToken ct = default);
    Task DeleteAsync(VenueEvent ev, CancellationToken ct = default);
    Task AddPhotoAsync(VenueEventPhoto photo, CancellationToken ct = default);
    Task DeletePhotoAsync(VenueEventPhoto photo, CancellationToken ct = default);
}
