using Hydra.Api.Data;
using Hydra.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Repositories.VenuePricing;

public class VenuePricingRepository : IVenuePricingRepository
{
    private readonly AppDbContext _context;

    public VenuePricingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task ReplaceAllAsync(Guid venueId, IReadOnlyList<VenuePricingItem> items, CancellationToken ct = default)
    {
        var existing = await _context.VenuePricingItems
            .Where(pi => pi.VenueId == venueId)
            .ToListAsync(ct);

        _context.VenuePricingItems.RemoveRange(existing);
        _context.VenuePricingItems.AddRange(items);
        await _context.SaveChangesAsync(ct);
    }
}
