using Hydra.Api.Models;

namespace Hydra.Api.Repositories.Venues;

public interface IVenueRepository
{
    Task<(List<Venue> Items, int TotalCount)> GetAllAsync(int skip, int take, Guid? venueTypeId = null, string? name = null, CancellationToken ct = default);
    Task<Venue?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Venue?> GetByUserIdAsync(Guid id, CancellationToken ct = default);
    Task<Venue?> GetByIdWithRulesAsync(Guid id, CancellationToken ct = default);
    Task<Venue> AddAsync(Venue venue, CancellationToken ct = default);
    Task UpdateAsync(Venue venue, CancellationToken ct = default);
    Task<BookingRules?> GetRulesByVenueIdAsync(Guid venueId, CancellationToken ct = default);
    Task UpdateRulesAsync(BookingRules rules, CancellationToken ct = default);
}