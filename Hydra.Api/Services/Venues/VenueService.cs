using Hydra.Api.Caching;
using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Mapping;
using Hydra.Api.Models;
using Hydra.Api.Repositories.Ratings;
using Hydra.Api.Repositories.VenuePhotos;
using Hydra.Api.Repositories.VenuePricing;
using Hydra.Api.Repositories.Venues;
using Hydra.Api.Services.GooglePlaces;

namespace Hydra.Api.Services.Venues;

public class VenueService : IVenueService
{
    private readonly IVenueRepository _venueRepo;
    private readonly IVenuePhotoRepository _photoRepo;
    private readonly IRatingRepository _ratingRepo;
    private readonly IVenuePricingRepository _pricingRepo;
    private readonly ICache _cache;
    private readonly IGooglePlacesService _googlePlacesService;

    public VenueService(
        IVenueRepository venueRepo,
        IVenuePhotoRepository photoRepo,
        IRatingRepository ratingRepo,
        IVenuePricingRepository pricingRepo,
        ICache cache,
        IGooglePlacesService googlePlacesService)
    {
        _venueRepo = venueRepo;
        _photoRepo = photoRepo;
        _ratingRepo = ratingRepo;
        _pricingRepo = pricingRepo;
        _cache = cache;
        _googlePlacesService = googlePlacesService;
    }

    public async Task<PagedResult<VenueDto>> GetAllVenuesAsync(int page, int pageSize, Guid? venueTypeId = null, string? name = null, string? location = null, CancellationToken ct = default)
    {
        var safeSize = Math.Clamp(pageSize, 1, 100);
        var skip = (Math.Max(1, page) - 1) * safeSize;
        var version = await _cache.GetTokenAsync(CacheKeys.VenuesToken, ct: ct);
        var key = CacheKeys.VenuesList(page, safeSize, venueTypeId, version, name, location);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.VenuesList,
            factory: async ct =>
            {
                var (items, total) = await _venueRepo.GetAllAsync(skip, safeSize, venueTypeId, name, location, ct);

                var venueIds = items.Select(v => v.Id);
                var ratingAggregates = await _ratingRepo.GetAggregatesAsync(venueIds, ct);

                var dtos = await Task.WhenAll(items.Select(async v =>
                {
                    ratingAggregates.TryGetValue(v.Id, out var agg);

                    var cover = v.Photos.MinBy(p => p.DisplayOrder);
                    if (cover is null)
                        return v.ToDto(averageRating: agg.Average, ratingCount: agg.Count);

                    var url = await _googlePlacesService.GetPhotoUrlAsync(cover.GooglePlaceId, ct: ct);
                    var photos = v.Photos
                        .OrderBy(p => p.DisplayOrder)
                        .Select(p => p.Id == cover.Id ? p.ToDto(url) : p.ToDto())
                        .ToList();
                    return v.ToDto(photos, agg.Average, agg.Count);
                }));

                return new PagedResult<VenueDto>(dtos.ToList(), total, page, safeSize);
            },
            jitter: CacheKeys.Jitter.Venues,
            ct: ct
        );
    }

    public async Task<IReadOnlyList<string>> GetLocationsAsync(CancellationToken ct = default)
    {
        var version = await _cache.GetTokenAsync(CacheKeys.VenuesToken, ct: ct);
        var key = CacheKeys.LocationsList(version);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.VenuesList,
            factory: async ct => (IReadOnlyList<string>)await _venueRepo.GetLocationsAsync(ct),
            jitter: CacheKeys.Jitter.Venues,
            ct: ct
        ) ?? [];
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
                var (avg, count) = await _ratingRepo.GetAggregateAsync(id, ct);
                return venue.ToDto(resolvedPhotos, avg, count);
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

    public async Task<BookingRulesDto?> GetBookingRulesAsync(Guid venueId, CancellationToken ct = default)
    {
        var rules = await _venueRepo.GetRulesByVenueIdAsync(venueId, ct);
        if (rules is null)
            return null;

        return new BookingRulesDto(rules.AutoConfirm, rules.SlotMinutes, rules.OpenHour, rules.CloseHour);
    }

    public async Task<BookingRulesDto?> UpdateBookingRulesAsync(Guid venueId, UpdateBookingRulesRequest request, CancellationToken ct = default)
    {
        var rules = await _venueRepo.GetRulesByVenueIdAsync(venueId, ct);
        if (rules is null)
            return null;

        rules.AutoConfirm = request.AutoConfirm;
        rules.SlotMinutes = request.SlotMinutes;
        rules.OpenHour = request.OpenHour;
        rules.CloseHour = request.CloseHour;

        await _venueRepo.UpdateRulesAsync(rules, ct);
        await _cache.BumpTokenAsync(CacheKeys.AvailabilityToken, ct);

        return new BookingRulesDto(rules.AutoConfirm, rules.SlotMinutes, rules.OpenHour, rules.CloseHour);
    }

    public async Task<VenueDto?> ToggleBookingsAsync(Guid venueId, bool enabled, CancellationToken ct = default)
    {
        var venue = await _venueRepo.GetByIdAsync(venueId, ct);
        if (venue is null) return null;
        venue.BookingsEnabled = enabled;
        await _venueRepo.UpdateAsync(venue, ct);
        await _cache.BumpTokenAsync(CacheKeys.VenuesToken, ct);
        return venue.ToDto();
    }

    public async Task<VenueDto?> ToggleEventsAsync(Guid venueId, bool enabled, CancellationToken ct = default)
    {
        var venue = await _venueRepo.GetByIdAsync(venueId, ct);
        if (venue is null) return null;
        venue.EventsEnabled = enabled;
        await _venueRepo.UpdateAsync(venue, ct);
        await _cache.BumpTokenAsync(CacheKeys.VenuesToken, ct);
        return venue.ToDto();
    }

    public async Task<IReadOnlyList<VenuePricingItemDto>?> SetVenuePricingAsync(Guid venueId, SetVenuePricingRequest request, CancellationToken ct = default)
    {
        var venue = await _venueRepo.GetByIdAsync(venueId, ct);
        if (venue is null)
            return null;

        var items = request.Items
            .Select((r, i) => new VenuePricingItem
            {
                VenueId = venueId,
                Category = r.Category,
                Title = r.Title,
                Subtitle = r.Subtitle,
                Price = r.Price,
                DisplayOrder = r.DisplayOrder
            })
            .ToList();

        await _pricingRepo.ReplaceAllAsync(venueId, items, ct);
        await _cache.BumpTokenAsync(CacheKeys.VenuesToken, ct);

        return items.Select(pi => pi.ToDto()).ToList();
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
