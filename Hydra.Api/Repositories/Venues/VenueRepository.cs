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

    public async Task<List<Venue>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Venues
            .Include(v => v.VenueType)
            .ToListAsync(ct);
    }

    public async Task<Venue?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Venues
            .Include(v => v.VenueType)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<Venue?> GetByIdWithRulesAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Venues
            .Include(v => v.VenueType)
            .Include(v => v.Rules)
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

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var venue = await _context.Venues.FindAsync(new object[] { id }, ct);
        if (venue is not null)
        {
            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync(ct);
        }
    }
}