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
            .AsNoTracking()
            .Include(v => v.VenueType)
            .ToListAsync(ct);
    }

    public async Task<Venue?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Venues
            .AsNoTracking()
            .Include(v => v.VenueType)
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
}