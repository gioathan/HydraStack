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

    public async Task<List<VenueTypeDto>> GetAllVenueTypesAsync(CancellationToken ct = default)
    {
        var venueTypes = await _venueTypeRepo.GetAllAsync(ct);
        return venueTypes.Select(vt => vt.ToDto()).ToList();
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