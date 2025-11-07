using Hydra.Api.Contracts.Venues;

namespace Hydra.Api.Services.Venues;

public interface IVenueService
{
    Task<List<VenueDto>> GetAllVenuesAsync(CancellationToken ct = default);
    Task<VenueDto?> GetVenueByIdAsync(Guid id, CancellationToken ct = default);
    Task<VenueDto> CreateVenueAsync(CreateVenueRequest request, CancellationToken ct = default);
    Task<VenueDto?> UpdateVenueAsync(Guid id, UpdateVenueRequest request, CancellationToken ct = default);
    Task<bool> DeleteVenueAsync(Guid id, CancellationToken ct = default);
}