using Hydra.Api.Contracts.Ratings;
using Hydra.Api.Data;
using Hydra.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Repositories.Ratings;

public class RatingRepository : IRatingRepository
{
    private readonly AppDbContext _context;

    public RatingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasRatedAsync(Guid customerId, Guid venueId, CancellationToken ct = default)
    {
        return await _context.VenueRatings
            .AnyAsync(r => r.CustomerId == customerId && r.VenueId == venueId, ct);
    }

    public async Task<(decimal Average, int Count)> GetAggregateAsync(Guid venueId, CancellationToken ct = default)
    {
        var agg = await _context.VenueRatings.AsNoTracking()
            .Where(r => r.VenueId == venueId)
            .GroupBy(_ => 1)
            .Select(g => new { Avg = (decimal?)g.Average(r => r.Value), Count = g.Count() })
            .FirstOrDefaultAsync(ct);

        return (agg?.Avg ?? 0m, agg?.Count ?? 0);
    }

    public async Task<Dictionary<Guid, (decimal Average, int Count)>> GetAggregatesAsync(
        IEnumerable<Guid> venueIds,
        CancellationToken ct = default)
    {
        var ids = venueIds.ToList();
        if (ids.Count == 0)
            return [];

        var rows = await _context.VenueRatings
            .Where(r => ids.Contains(r.VenueId))
            .GroupBy(r => r.VenueId)
            .Select(g => new { VenueId = g.Key, Average = g.Average(r => r.Value), Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.VenueId, x => ((decimal)x.Average, x.Count));
    }

    public async Task<List<PendingRatingDto>> GetPendingRatingsAsync(Guid customerId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // One pending rating per venue — keep most recent eligible booking.
        // The NOT-EXISTS on a later booking for the same venue collapses the
        // dedupe into SQL so we don't materialize every past booking.
        return await _context.Bookings
            .AsNoTracking()
            .Where(b =>
                b.CustomerId == customerId &&
                b.Status == BookingStatus.Confirmed &&
                b.EndUtc < now &&
                !_context.VenueRatings.Any(r => r.CustomerId == customerId && r.VenueId == b.VenueId) &&
                !_context.Bookings.Any(later =>
                    later.CustomerId == customerId &&
                    later.Status == BookingStatus.Confirmed &&
                    later.EndUtc < now &&
                    later.VenueId == b.VenueId &&
                    later.EndUtc > b.EndUtc))
            .OrderByDescending(b => b.EndUtc)
            .Select(b => new PendingRatingDto(b.VenueId, b.Venue.Name, b.Id, b.EndUtc))
            .ToListAsync(ct);
    }

    public async Task AddAsync(VenueRating rating, CancellationToken ct = default)
    {
        _context.VenueRatings.Add(rating);
        await _context.SaveChangesAsync(ct);
    }
}
