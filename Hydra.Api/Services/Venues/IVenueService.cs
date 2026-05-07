using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Venues;

namespace Hydra.Api.Services.Venues;

public interface IVenueService
{
    Task<PagedResult<VenueDto>> GetAllVenuesAsync(int page, int pageSize, Guid? venueTypeId = null, CancellationToken ct = default);
    Task<VenueDto?> GetVenueByIdAsync(Guid id, CancellationToken ct = default);
    Task<VenueDto> CreateVenueAsync(CreateVenueRequest request, CancellationToken ct = default);
    Task<VenueDto?> UpdateVenueAsync(Guid id, UpdateVenueRequest request, CancellationToken ct = default);
    Task<VenuePhotoDto?> AddPhotoAsync(Guid venueId, AddVenuePhotoRequest request, CancellationToken ct = default);
    Task<bool> DeletePhotoAsync(Guid venueId, Guid photoId, CancellationToken ct = default);
    Task<IReadOnlyList<VenuePhotoDto>?> ReorderPhotosAsync(Guid venueId, ReorderVenuePhotosRequest request, CancellationToken ct = default);
}
