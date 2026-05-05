using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.VenueTypes;
using Hydra.Api.Mapping;
using Hydra.Api.Repositories.VenueTypes;

namespace Hydra.Api.Services.VenueTypes;

public class VenueTypeService : IVenueTypeService
{
    private readonly IVenueTypeRepository _venueTypeRepo;

    public VenueTypeService(IVenueTypeRepository venueTypeRepo)
    {
        _venueTypeRepo = venueTypeRepo;
    }

    public async Task<PagedResult<VenueTypeDto>> GetAllVenueTypesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var safeSize = Math.Clamp(pageSize, 1, 100);
        var skip = (Math.Max(1, page) - 1) * safeSize;
        var (items, total) = await _venueTypeRepo.GetAllAsync(skip, safeSize, ct);
        return new PagedResult<VenueTypeDto>(items.Select(vt => vt.ToDto()).ToList(), total, page, safeSize);
    }

    public async Task<VenueTypeDto?> GetVenueTypeByIdAsync(Guid id, CancellationToken ct = default)
    {
        var venueType = await _venueTypeRepo.GetByIdAsync(id, ct);
        return venueType?.ToDto();
    }

    public async Task<VenueTypeDto> CreateVenueTypeAsync(CreateVenueTypeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Name is required");
        }

        var venueType = request.ToModel();
        var created = await _venueTypeRepo.AddAsync(venueType, ct);

        return created.ToDto();
    }

    public async Task<VenueTypeDto?> UpdateVenueTypeAsync(Guid id, UpdateVenueTypeRequest request, CancellationToken ct = default)
    {
        var venueType = await _venueTypeRepo.GetByIdAsync(id, ct);
        if (venueType is null)
            return null;

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Name is required");
        }

        venueType.UpdateFrom(request);
        await _venueTypeRepo.UpdateAsync(venueType, ct);

        return venueType.ToDto();
    }

    public async Task<bool> DeleteVenueTypeAsync(Guid id, CancellationToken ct = default)
    {
        var venueType = await _venueTypeRepo.GetByIdAsync(id, ct);
        if (venueType is null)
            return false;

        await _venueTypeRepo.DeleteAsync(id, ct);
        return true;
    }
}