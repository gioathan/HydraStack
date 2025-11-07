using Hydra.Api.Models;

namespace Hydra.Api.Repositories.Venues;

public interface IVenueRepository
{
    Task<List<Venue>> GetAllAsync(CancellationToken ct = default);
    Task<Venue?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Venue?> GetByIdWithRulesAsync(Guid id, CancellationToken ct = default);
    Task<Venue> AddAsync(Venue venue, CancellationToken ct = default);
    Task UpdateAsync(Venue venue, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}