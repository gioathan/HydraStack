using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Venues;

namespace Hydra.Api.Services.Venues;

public interface IVenueService
{
    Task<PagedResult<VenueDto>> GetAllVenuesAsync(int page, int pageSize, CancellationToken ct = default);
    Task<VenueDto?> GetVenueByIdAsync(Guid id, CancellationToken ct = default);
    Task<VenueDto> CreateVenueAsync(CreateVenueRequest request, CancellationToken ct = default);
    Task<VenueDto?> UpdateVenueAsync(Guid id, UpdateVenueRequest request, CancellationToken ct = default);
}