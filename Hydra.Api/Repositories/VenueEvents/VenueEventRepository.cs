using Hydra.Api.Data;
using Hydra.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Repositories.VenueEvents;

public class VenueEventRepository : IVenueEventRepository
{
    private readonly AppDbContext _context;

    public VenueEventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<VenueEvent>> GetByVenueIdAsync(Guid venueId, bool includePast, CancellationToken ct = default)
    {
        var query = _context.VenueEvents
            .Include(e => e.AdditionalPhotos)
            .Where(e => e.VenueId == venueId);

        if (!includePast)
            query = query.Where(e =>
                !e.ClosedAtUtc.HasValue &&
                (!e.EndsAtUtc.HasValue || e.EndsAtUtc.Value >= DateTime.UtcNow));

        return await query
            .OrderBy(e => e.StartsAtUtc)
            .ToListAsync(ct);
    }

    public async Task<(List<VenueEvent> Items, int Total)> GetUpcomingPagedAsync(int skip, int take, string? location, CancellationToken ct = default)
    {
        var query = _context.VenueEvents
            .Include(e => e.AdditionalPhotos)
            .Include(e => e.Venue)
            .Where(e =>
                !e.ClosedAtUtc.HasValue &&
                (!e.EndsAtUtc.HasValue || e.EndsAtUtc.Value >= DateTime.UtcNow) &&
                e.StartsAtUtc >= DateTime.UtcNow &&
                e.Venue.EventsEnabled);

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(e => e.Venue.Location == location);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.StartsAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<VenueEvent?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.VenueEvents
            .Include(e => e.AdditionalPhotos)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<bool> HasActiveEventOnDayAsync(Guid venueId, DateTime date, Guid? excludeId, CancellationToken ct = default)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        return await _context.VenueEvents.AnyAsync(e =>
            e.VenueId == venueId &&
            (excludeId == null || e.Id != excludeId) &&
            !e.ClosedAtUtc.HasValue &&
            (!e.EndsAtUtc.HasValue || e.EndsAtUtc.Value >= DateTime.UtcNow) &&
            e.StartsAtUtc >= dayStart &&
            e.StartsAtUtc < dayEnd,
            ct);
    }

    public async Task<VenueEvent> AddAsync(VenueEvent ev, CancellationToken ct = default)
    {
        _context.VenueEvents.Add(ev);
        await _context.SaveChangesAsync(ct);
        return ev;
    }

    public async Task UpdateAsync(VenueEvent ev, CancellationToken ct = default)
    {
        _context.VenueEvents.Update(ev);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(VenueEvent ev, CancellationToken ct = default)
    {
        _context.VenueEvents.Remove(ev);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddPhotoAsync(VenueEventPhoto photo, CancellationToken ct = default)
    {
        _context.VenueEventPhotos.Add(photo);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeletePhotoAsync(VenueEventPhoto photo, CancellationToken ct = default)
    {
        _context.VenueEventPhotos.Remove(photo);
        await _context.SaveChangesAsync(ct);
    }
}
