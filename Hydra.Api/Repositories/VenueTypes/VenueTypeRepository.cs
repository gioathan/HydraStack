using Hydra.Api.Data;
using Hydra.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Repositories.VenueTypes;

public class VenueTypeRepository : IVenueTypeRepository
{
    private readonly AppDbContext _context;

    public VenueTypeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<VenueType>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.VenueTypes
            .OrderBy(vt => vt.DisplayOrder)
            .ThenBy(vt => vt.Name)
            .ToListAsync(ct);
    }

    public async Task<VenueType?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.VenueTypes
            .FirstOrDefaultAsync(vt => vt.Id == id, ct);
    }

    public async Task<VenueType> AddAsync(VenueType venueType, CancellationToken ct = default)
    {
        _context.VenueTypes.Add(venueType);
        await _context.SaveChangesAsync(ct);
        return venueType;
    }

    public async Task UpdateAsync(VenueType venueType, CancellationToken ct = default)
    {
        _context.VenueTypes.Update(venueType);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var venueType = await _context.VenueTypes.FindAsync(new object[] { id }, ct);
        if (venueType is not null)
        {
            _context.VenueTypes.Remove(venueType);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.VenueTypes.AnyAsync(vt => vt.Id == id, ct);
    }
}