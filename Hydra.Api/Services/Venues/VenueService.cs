using Hydra.Api.Caching;
using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Mapping;
using Hydra.Api.Models;
using Hydra.Api.Repositories.VenuePhotos;
using Hydra.Api.Repositories.Venues;
using Hydra.Api.Services.GooglePlaces;

namespace Hydra.Api.Services.Venues;

public class VenueService : IVenueService
{
    private readonly IVenueRepository _venueRepo;
    private readonly IVenuePhotoRepository _photoRepo;
    private readonly ICache _cache;
    private readonly IGooglePlacesService _googlePlacesService;

    public VenueService(
        IVenueRepository venueRepo,
        IVenuePhotoRepository photoRepo,
        ICache cache,
        IGooglePlacesService googlePlacesService)
    {
        _venueRepo = venueRepo;
        _photoRepo = photoRepo;
        _cache = cache;
        _googlePlacesService = googlePlacesService;
    }

    public async Task<PagedResult<VenueDto>> GetAllVenuesAsync(int page, int pageSize, Guid? venueTypeId = null, string? name = null, CancellationToken ct = default)
    {
        var safeSize = Math.Clamp(pageSize, 1, 100);
        var skip = (Math.Max(1, page) - 1) * safeSize;
        var version = await _cache.GetTokenAsync(CacheKeys.VenuesToken, ct: ct);
        var key = CacheKeys.VenuesList(page, safeSize, venueTypeId, version, name);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.VenuesList,
            factory: async ct =>
            {
                var (items, total) = await _venueRepo.GetAllAsync(skip, safeSize, venueTypeId, name, ct);

                var dtos = await Task.WhenAll(items.Select(async v =>
                {
                    var cover = v.Photos.MinBy(p => p.DisplayOrder);
                    if (cover is null)
                        return v.ToDto();

                    var url = await _googlePlacesService.GetPhotoUrlAsync(cover.GooglePlaceId, ct: ct);
                    var photos = v.Photos
                        .OrderBy(p => p.DisplayOrder)
                        .Select(p => p.Id == cover.Id ? p.ToDto(url) : p.ToDto())
                        .ToList();
                    return v.ToDto(photos);
                }));

                return new PagedResult<VenueDto>(dtos.ToList(), total, page, safeSize);
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
                if (venue is null)
                    return null;

                var resolvedPhotos = await ResolvePhotoUrlsAsync(venue.Photos, ct);
                return venue.ToDto(resolvedPhotos);
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

    public async Task<VenuePhotoDto?> AddPhotoAsync(Guid venueId, AddVenuePhotoRequest request, CancellationToken ct = default)
    {
        var venue = await _venueRepo.GetByIdAsync(venueId, ct);
        if (venue is null)
            return null;

        var photo = new VenuePhoto
        {
            VenueId = venueId,
            GooglePlaceId = request.GooglePlaceId,
            DisplayOrder = request.DisplayOrder
        };

        await _photoRepo.AddAsync(photo, ct);
        await _cache.BumpTokenAsync(CacheKeys.VenuesToken, ct);

        var url = await _googlePlacesService.GetPhotoUrlAsync(photo.GooglePlaceId, ct: ct);
        return photo.ToDto(url);
    }

    public async Task<bool> DeletePhotoAsync(Guid venueId, Guid photoId, CancellationToken ct = default)
    {
        var photo = await _photoRepo.GetByIdAsync(photoId, ct);
        if (photo is null || photo.VenueId != venueId)
            return false;

        await _photoRepo.DeleteAsync(photo, ct);
        await _cache.BumpTokenAsync(CacheKeys.VenuesToken, ct);
        return true;
    }

    public async Task<IReadOnlyList<VenuePhotoDto>?> ReorderPhotosAsync(Guid venueId, ReorderVenuePhotosRequest request, CancellationToken ct = default)
    {
        var venue = await _venueRepo.GetByIdAsync(venueId, ct);
        if (venue is null)
            return null;

        var photoMap = venue.Photos.ToDictionary(p => p.Id);
        var toUpdate = new List<VenuePhoto>();

        foreach (var item in request.Items)
        {
            if (photoMap.TryGetValue(item.PhotoId, out var photo))
            {
                photo.DisplayOrder = item.DisplayOrder;
                toUpdate.Add(photo);
            }
        }

        if (toUpdate.Count > 0)
            await _photoRepo.UpdateRangeAsync(toUpdate, ct);

        await _cache.BumpTokenAsync(CacheKeys.VenuesToken, ct);

        return venue.Photos
            .OrderBy(p => p.DisplayOrder)
            .Select(p => p.ToDto())
            .ToList();
    }

    private async Task<IReadOnlyList<VenuePhotoDto>> ResolvePhotoUrlsAsync(
        IEnumerable<VenuePhoto> photos,
        CancellationToken ct)
    {
        var ordered = photos.OrderBy(p => p.DisplayOrder).ToList();
        var tasks = ordered.Select(async p =>
        {
            var url = await _googlePlacesService.GetPhotoUrlAsync(p.GooglePlaceId, ct: ct);
            return p.ToDto(url);
        });
        return await Task.WhenAll(tasks);
    }
}
