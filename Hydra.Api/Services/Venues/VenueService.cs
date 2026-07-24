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

                    if (v.Photos.Count == 0)
                        return v.ToDto(averageRating: agg.Average, ratingCount: agg.Count);

                    var photos = await ResolvePhotoUrlsAsync(v.Photos, ct);
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

                var ratingsVersion = await _cache.GetTokenAsync(CacheKeys.RatingsToken, ct: ct);
                var ratingKey = CacheKeys.RatingAggregate(id, ratingsVersion);
                var ratingEntry = await _cache.GetOrSetAsync(
                    key: ratingKey,
                    ttl: CacheKeys.Ttl.RatingAggregate,
                    factory: async ct =>
                    {
                        var (avg, count) = await _ratingRepo.GetAggregateAsync(id, ct);
                        return new RatingCacheEntry(avg, count);
                    },
                    jitter: CacheKeys.Jitter.Ratings,
                    ct: ct);

                return venue.ToDto(resolvedPhotos, ratingEntry?.Average ?? 0m, ratingEntry?.Count ?? 0);
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

        // Every venue needs a rules row to exist before its admin can configure
        // booking hours/auto-confirm at all — create one with sensible defaults
        // up front rather than leaving it to be created lazily (which previously
        // never happened, so the rules form 404'd forever).
        await _venueRepo.AddRulesAsync(new BookingRules { VenueId = created.Id }, ct);

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

    public async Task<VenueDto?> ToggleBookingsAsync(Guid venueId, bool enabled, CancellationToken ct = default)
    {
        var venue = await _venueRepo.GetByIdAsync(venueId, ct);
        if (venue is null)
            return null;

        venue.BookingsEnabled = enabled;
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
            GooglePlaceId = "",
            Url = request.Url,
            DisplayOrder = request.DisplayOrder
        };

        await _photoRepo.AddAsync(photo, ct);
        await _cache.BumpTokenAsync(CacheKeys.VenuesToken, ct);

        return photo.ToDto();
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

        return rules.ToDto();
    }

    public async Task<BookingRulesDto?> UpdateBookingRulesAsync(Guid venueId, UpdateBookingRulesRequest request, CancellationToken ct = default)
    {
        var rules = await _venueRepo.GetRulesByVenueIdAsync(venueId, ct);

        // Venues created before rules rows were seeded on creation would 404
        // here forever with no way to self-serve a fix — create the row instead
        // of just failing, so saving the form in the admin UI always works.
        if (rules is null)
        {
            var venue = await _venueRepo.GetByIdAsync(venueId, ct);
            if (venue is null)
                return null;

            rules = new BookingRules { VenueId = venueId };
            await _venueRepo.AddRulesAsync(rules.ApplyFrom(request), ct);
        }
        else
        {
            await _venueRepo.UpdateRulesAsync(rules.ApplyFrom(request), ct);
        }

        // VenueDto (the customer-facing venue detail response) surfaces these
        // hours, so its cache must be invalidated too — not just availability.
        await _cache.BumpTokenAsync(CacheKeys.VenuesToken, ct);
        await _cache.BumpTokenAsync(CacheKeys.AvailabilityToken, ct);

        return rules.ToDto();
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

    private async Task<string?> ResolvePhotoUrlAsync(VenuePhoto photo, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(photo.Url))
            return photo.Url;

        if (!string.IsNullOrEmpty(photo.GooglePlaceId))
            return await _googlePlacesService.GetPhotoUrlAsync(photo.GooglePlaceId, ct: ct);

        return null;
    }

    private async Task<IReadOnlyList<VenuePhotoDto>> ResolvePhotoUrlsAsync(
        IEnumerable<VenuePhoto> photos,
        CancellationToken ct)
    {
        var ordered = photos.OrderBy(p => p.DisplayOrder).ToList();
        var tasks = ordered.Select(async p =>
        {
            var url = await ResolvePhotoUrlAsync(p, ct);
            return p.ToDto(url);
        });
        return await Task.WhenAll(tasks);
    }
}
