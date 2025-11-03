using Hydra.Api.Contracts.VenueTypes;

namespace Hydra.Api.Services.VenueTypes;

public interface IVenueTypeService
{
    Task<List<VenueTypeDto>> GetAllVenueTypesAsync(CancellationToken ct = default);
    Task<VenueTypeDto?> GetVenueTypeByIdAsync(Guid id, CancellationToken ct = default);
    Task<VenueTypeDto> CreateVenueTypeAsync(CreateVenueTypeRequest request, CancellationToken ct = default);
    Task<VenueTypeDto?> UpdateVenueTypeAsync(Guid id, UpdateVenueTypeRequest request, CancellationToken ct = default);
    Task<bool> DeleteVenueTypeAsync(Guid id, CancellationToken ct = default);
}