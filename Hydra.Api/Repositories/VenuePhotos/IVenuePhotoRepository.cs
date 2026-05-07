using Hydra.Api.Models;

namespace Hydra.Api.Repositories.VenuePhotos;

public interface IVenuePhotoRepository
{
    Task<VenuePhoto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<VenuePhoto> AddAsync(VenuePhoto photo, CancellationToken ct = default);
    Task DeleteAsync(VenuePhoto photo, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<VenuePhoto> photos, CancellationToken ct = default);
}
