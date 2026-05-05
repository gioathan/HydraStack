using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.VenueTypes;

namespace Hydra.Api.Services.VenueTypes;

public interface IVenueTypeService
{
    Task<PagedResult<VenueTypeDto>> GetAllVenueTypesAsync(int page, int pageSize, CancellationToken ct = default);
    Task<VenueTypeDto?> GetVenueTypeByIdAsync(Guid id, CancellationToken ct = default);
    Task<VenueTypeDto> CreateVenueTypeAsync(CreateVenueTypeRequest request, CancellationToken ct = default);
    Task<VenueTypeDto?> UpdateVenueTypeAsync(Guid id, UpdateVenueTypeRequest request, CancellationToken ct = default);
    Task<bool> DeleteVenueTypeAsync(Guid id, CancellationToken ct = default);
}