using Hydra.Api.Data;
using Hydra.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Repositories.Venues;

public class VenueRepository : IVenueRepository
{
    private readonly AppDbContext _context;

    public VenueRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Venue> Items, int TotalCount)> GetAllAsync(int skip, int take, Guid? venueTypeId = null, string? name = null, string? location = null, CancellationToken ct = default)
    {
        var query = _context.Venues
            .AsNoTracking()
            .Include(v => v.VenueType)
            .Include(v => v.Photos.OrderBy(p => p.DisplayOrder))
            .Where(v => venueTypeId == null || v.VenueTypeId == venueTypeId)
            .Where(v => name == null || v.Name.ToLower().Contains(name.ToLower()))
            .Where(v => location == null || v.Location != null && v.Location.ToLower() == location.ToLower())
            .OrderBy(v => v.Name);

        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public async Task<List<string>> GetLocationsAsync(CancellationToken ct = default)
    {
        return await _context.Venues
            .AsNoTracking()
            .Where(v => v.Location != null)
            .Select(v => v.Location!)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync(ct);
    }

    public async Task<Venue?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Venues
            .AsNoTracking()
            .Include(v => v.VenueType)
            .Include(v => v.Photos.OrderBy(p => p.DisplayOrder))
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<Venue?> GetByUserIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Venues
            .AsNoTracking()
            .Include(v => v.VenueType)
            .FirstOrDefaultAsync(v => v.UserId == id, ct);
    }

    public async Task<Venue?> GetByIdWithRulesAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Venues
            .AsNoTracking()
            .Include(v => v.VenueType)
            .Include(v => v.Rules)
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<Venue> AddAsync(Venue venue, CancellationToken ct = default)
    {
        _context.Venues.Add(venue);
        await _context.SaveChangesAsync(ct);
        return venue;
    }

    public async Task UpdateAsync(Venue venue, CancellationToken ct = default)
    {
        _context.Venues.Update(venue);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<BookingRules?> GetRulesByVenueIdAsync(Guid venueId, CancellationToken ct = default)
    {
        return await _context.BookingRules
            .FirstOrDefaultAsync(r => r.VenueId == venueId, ct);
    }

    public async Task UpdateRulesAsync(BookingRules rules, CancellationToken ct = default)
    {
        _context.BookingRules.Update(rules);
        await _context.SaveChangesAsync(ct);
    }
}
