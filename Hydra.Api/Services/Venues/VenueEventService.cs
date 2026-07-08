using Hydra.Api.Caching;
using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Mapping;
using Hydra.Api.Models;
using Hydra.Api.Repositories.VenueEvents;
using Hydra.Api.Repositories.Venues;

namespace Hydra.Api.Services.Venues;

public class VenueEventService : IVenueEventService
{
    private readonly IVenueEventRepository _eventRepo;
    private readonly IVenueRepository _venueRepo;
    private readonly ICache _cache;

    public VenueEventService(IVenueEventRepository eventRepo, IVenueRepository venueRepo, ICache cache)
    {
        _eventRepo = eventRepo;
        _venueRepo = venueRepo;
        _cache = cache;
    }

    public async Task<PagedResult<EventListItemDto>> GetUpcomingPagedAsync(int page, int pageSize, string? location, CancellationToken ct = default)
    {
        var safeSize = Math.Clamp(pageSize, 1, 100);
        var skip = (Math.Max(1, page) - 1) * safeSize;

        var version = await _cache.GetTokenAsync(CacheKeys.EventsToken, ct: ct);
        var key = CacheKeys.UpcomingEvents(page, safeSize, location, version);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.EventsList,
            factory: async ct =>
            {
                var (items, total) = await _eventRepo.GetUpcomingPagedAsync(skip, safeSize, location, ct);

                var dtos = items.Select(e => new EventListItemDto(
                    e.Id,
                    e.VenueId,
                    e.Venue.Name,
                    e.Venue.Location,
                    e.Title,
                    e.Description,
                    e.StartsAtUtc,
                    e.EndsAtUtc,
                    e.MainPhotoUrl
                )).ToList();

                return new PagedResult<EventListItemDto>(dtos, total, page, safeSize);
            },
            jitter: CacheKeys.Jitter.Events,
            ct: ct
        );
    }

    public async Task<IReadOnlyList<VenueEventDto>> GetEventsAsync(Guid venueId, bool includePast, CancellationToken ct = default)
    {
        var version = await _cache.GetTokenAsync(CacheKeys.EventsToken, ct: ct);
        var key = CacheKeys.VenueEvents(venueId, includePast, version);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.EventsList,
            factory: async ct =>
            {
                var events = await _eventRepo.GetByVenueIdAsync(venueId, includePast, ct);
                return (IReadOnlyList<VenueEventDto>)events.Select(e => e.ToDto()).ToList();
            },
            jitter: CacheKeys.Jitter.Events,
            ct: ct
        ) ?? [];
    }

    public async Task<VenueEventDto?> GetEventByIdAsync(Guid venueId, Guid eventId, CancellationToken ct = default)
    {
        var version = await _cache.GetTokenAsync(CacheKeys.EventsToken, ct: ct);
        var key = CacheKeys.EventDetail(venueId, eventId, version);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.EventDetail,
            factory: async ct =>
            {
                var ev = await _eventRepo.GetByIdAsync(eventId, ct);
                if (ev is null || ev.VenueId != venueId) return null;
                return ev.ToDto();
            },
            cacheNull: true,
            jitter: CacheKeys.Jitter.Events,
            ct: ct
        );
    }

    public async Task<(VenueEventDto? Result, string? Error)> CreateEventAsync(Guid venueId, CreateVenueEventRequest request, CancellationToken ct = default)
    {
        var venue = await _venueRepo.GetByIdAsync(venueId, ct);
        if (venue is null)
            return (null, "Venue not found.");

        var day = request.StartsAtUtc.Date;
        if (await _eventRepo.HasActiveEventOnDayAsync(venueId, day, null, ct))
            return (null, "An active event already exists on this day for this venue.");

        var ev = new VenueEvent
        {
            VenueId = venueId,
            Title = request.Title,
            Description = request.Description,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            MainPhotoUrl = request.MainPhotoUrl,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _eventRepo.AddAsync(ev, ct);
        await _cache.BumpTokenAsync(CacheKeys.EventsToken, ct);
        return (ev.ToDto(), null);
    }

    public async Task<(VenueEventDto? Result, string? Error)> UpdateEventAsync(Guid venueId, Guid eventId, UpdateVenueEventRequest request, CancellationToken ct = default)
    {
        var ev = await _eventRepo.GetByIdAsync(eventId, ct);
        if (ev is null || ev.VenueId != venueId)
            return (null, "Event not found.");

        var day = request.StartsAtUtc.Date;
        if (await _eventRepo.HasActiveEventOnDayAsync(venueId, day, eventId, ct))
            return (null, "An active event already exists on this day for this venue.");

        ev.Title = request.Title;
        ev.Description = request.Description;
        ev.StartsAtUtc = request.StartsAtUtc;
        ev.EndsAtUtc = request.EndsAtUtc;
        ev.MainPhotoUrl = request.MainPhotoUrl;

        await _eventRepo.UpdateAsync(ev, ct);
        await _cache.BumpTokenAsync(CacheKeys.EventsToken, ct);
        return (ev.ToDto(), null);
    }

    public async Task<bool> DeleteEventAsync(Guid venueId, Guid eventId, CancellationToken ct = default)
    {
        var ev = await _eventRepo.GetByIdAsync(eventId, ct);
        if (ev is null || ev.VenueId != venueId) return false;
        await _eventRepo.DeleteAsync(ev, ct);
        await _cache.BumpTokenAsync(CacheKeys.EventsToken, ct);
        return true;
    }

    public async Task<VenueEventDto?> CloseEventAsync(Guid venueId, Guid eventId, CancellationToken ct = default)
    {
        var ev = await _eventRepo.GetByIdAsync(eventId, ct);
        if (ev is null || ev.VenueId != venueId) return null;

        ev.ClosedAtUtc = DateTime.UtcNow;
        await _eventRepo.UpdateAsync(ev, ct);
        await _cache.BumpTokenAsync(CacheKeys.EventsToken, ct);
        return ev.ToDto();
    }

    public async Task<VenueEventDto?> AddEventPhotoAsync(Guid venueId, Guid eventId, AddEventPhotoRequest request, CancellationToken ct = default)
    {
        var ev = await _eventRepo.GetByIdAsync(eventId, ct);
        if (ev is null || ev.VenueId != venueId) return null;

        var photo = new VenueEventPhoto
        {
            VenueEventId = eventId,
            Url = request.Url,
            DisplayOrder = request.DisplayOrder
        };

        await _eventRepo.AddPhotoAsync(photo, ct);
        ev.AdditionalPhotos.Add(photo);
        await _cache.BumpTokenAsync(CacheKeys.EventsToken, ct);
        return ev.ToDto();
    }

    public async Task<bool> DeleteEventPhotoAsync(Guid venueId, Guid eventId, Guid photoId, CancellationToken ct = default)
    {
        var ev = await _eventRepo.GetByIdAsync(eventId, ct);
        if (ev is null || ev.VenueId != venueId) return false;

        var photo = ev.AdditionalPhotos.FirstOrDefault(p => p.Id == photoId);
        if (photo is null) return false;

        await _eventRepo.DeletePhotoAsync(photo, ct);
        await _cache.BumpTokenAsync(CacheKeys.EventsToken, ct);
        return true;
    }
}
