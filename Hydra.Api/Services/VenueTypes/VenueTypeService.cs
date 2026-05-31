using Hydra.Api.Caching;
using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.VenueTypes;
using Hydra.Api.Mapping;
using Hydra.Api.Repositories.VenueTypes;

namespace Hydra.Api.Services.VenueTypes;

public class VenueTypeService : IVenueTypeService
{
    private readonly IVenueTypeRepository _venueTypeRepo;
    private readonly ICache _cache;

    public VenueTypeService(IVenueTypeRepository venueTypeRepo, ICache cache)
    {
        _venueTypeRepo = venueTypeRepo;
        _cache = cache;
    }

    public async Task<PagedResult<VenueTypeDto>> GetAllVenueTypesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var safeSize = Math.Clamp(pageSize, 1, 100);
        var skip = (Math.Max(1, page) - 1) * safeSize;
        var version = await _cache.GetTokenAsync(CacheKeys.VenueTypesToken, ct: ct);
        var key = CacheKeys.VenueTypesList(page, safeSize, version);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.VenueTypesList,
            factory: async ct =>
            {
                var (items, total) = await _venueTypeRepo.GetAllAsync(skip, safeSize, ct);
                return new PagedResult<VenueTypeDto>(items.Select(vt => vt.ToDto()).ToList(), total, page, safeSize);
            },
            jitter: CacheKeys.Jitter.VenueTypes,
            ct: ct);
    }

    public async Task<VenueTypeDto?> GetVenueTypeByIdAsync(Guid id, CancellationToken ct = default)
    {
        var version = await _cache.GetTokenAsync(CacheKeys.VenueTypesToken, ct: ct);
        var key = CacheKeys.VenueTypeDetail(id, version);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.VenueTypesList,
            factory: async ct =>
            {
                var venueType = await _venueTypeRepo.GetByIdAsync(id, ct);
                return venueType?.ToDto();
            },
            cacheNull: true,
            jitter: CacheKeys.Jitter.VenueTypes,
            ct: ct);
    }

    public async Task<VenueTypeDto> CreateVenueTypeAsync(CreateVenueTypeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required");

        var venueType = request.ToModel();
        var created = await _venueTypeRepo.AddAsync(venueType, ct);
        await _cache.BumpTokenAsync(CacheKeys.VenueTypesToken, ct);
        return created.ToDto();
    }

    public async Task<VenueTypeDto?> UpdateVenueTypeAsync(Guid id, UpdateVenueTypeRequest request, CancellationToken ct = default)
    {
        var venueType = await _venueTypeRepo.GetByIdAsync(id, ct);
        if (venueType is null)
            return null;

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required");

        venueType.UpdateFrom(request);
        await _venueTypeRepo.UpdateAsync(venueType, ct);
        await _cache.BumpTokenAsync(CacheKeys.VenueTypesToken, ct);
        return venueType.ToDto();
    }

    public async Task<bool> DeleteVenueTypeAsync(Guid id, CancellationToken ct = default)
    {
        var venueType = await _venueTypeRepo.GetByIdAsync(id, ct);
        if (venueType is null)
            return false;

        await _venueTypeRepo.DeleteAsync(id, ct);
        await _cache.BumpTokenAsync(CacheKeys.VenueTypesToken, ct);
        return true;
    }
}
