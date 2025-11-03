using Hydra.Api.Models;

namespace Hydra.Api.Repositories.VenueTypes;

public interface IVenueTypeRepository
{
    Task<List<VenueType>> GetAllAsync(CancellationToken ct = default);
    Task<VenueType?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<VenueType> AddAsync(VenueType venueType, CancellationToken ct = default);
    Task UpdateAsync(VenueType venueType, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}