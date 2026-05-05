using Hydra.Api.Caching;
using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Mapping;
using Hydra.Api.Repositories.Venues;

namespace Hydra.Api.Services.Venues;

public class VenueService : IVenueService
{
    private readonly IVenueRepository _venueRepo;
    private readonly ICache _cache;

    public VenueService(IVenueRepository venueRepo, ICache cache)
    {
        _venueRepo = venueRepo;
        _cache = cache;
    }

    public async Task<PagedResult<VenueDto>> GetAllVenuesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var safeSize = Math.Clamp(pageSize, 1, 100);
        var skip = (Math.Max(1, page) - 1) * safeSize;
        var version = await _cache.GetTokenAsync(CacheKeys.VenuesToken, ct: ct);
        var key = CacheKeys.VenuesList(page, safeSize, version);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.VenuesList,
            factory: async ct =>
            {
                var (items, total) = await _venueRepo.GetAllAsync(skip, safeSize, ct);
                return new PagedResult<VenueDto>(items.Select(v => v.ToDto()).ToList(), total, page, safeSize);
            },
            jitter: CacheKeys.Jitter.Venues,
            ct: ct
        );
    }

    public async Task<VenueDto?> GetVenueByIdAsync(Guid id, CancellationToken ct = default)
    {
        var version = await _cache.GetTokenAsync(CacheKeys.VenuesToken, ct: ct);
        var key = CacheKeys.VenueDetail(id, version);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.VenueDetail,
            factory: async ct =>
            {
                var venue = await _venueRepo.GetByIdAsync(id, ct);
                return venue?.ToDto();
            },
            cacheNull: true,
            jitter: CacheKeys.Jitter.Venues,
            ct: ct
        );
    }

    public async Task<VenueDto> CreateVenueAsync(CreateVenueRequest request, CancellationToken ct = default)
    {
        var venue = request.ToModel();
        var created = await _venueRepo.AddAsync(venue, ct);

        await _cache.BumpTokenAsync(CacheKeys.VenuesToken, ct);

        return created.ToDto();
    }

    public async Task<VenueDto?> UpdateVenueAsync(Guid id, UpdateVenueRequest request, CancellationToken ct = default)
    {
        var venue = await _venueRepo.GetByIdAsync(id, ct);
        if (venue is null)
            return null;

        venue.UpdateFrom(request);
        await _venueRepo.UpdateAsync(venue, ct);

        await _cache.BumpTokenAsync(CacheKeys.VenuesToken, ct);
        await _cache.BumpTokenAsync(CacheKeys.AvailabilityToken, ct);

        return venue.ToDto();
    }
}