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
        var ratings = await _context.VenueRatings
            .Where(r => r.VenueId == venueId)
            .ToListAsync(ct);

        if (ratings.Count == 0)
            return (0m, 0);

        return (ratings.Average(r => r.Value), ratings.Count);
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

        var bookings = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Venue)
            .Where(b =>
                b.CustomerId == customerId &&
                b.Status == BookingStatus.Confirmed &&
                b.EndUtc < now &&
                !_context.VenueRatings.Any(r => r.CustomerId == customerId && r.VenueId == b.VenueId))
            .OrderByDescending(b => b.EndUtc)
            .ToListAsync(ct);

        // One pending rating per venue — keep most recent eligible booking
        return bookings
            .DistinctBy(b => b.VenueId)
            .Select(b => new PendingRatingDto(b.VenueId, b.Venue.Name, b.Id, b.EndUtc))
            .ToList();
    }

    public async Task AddAsync(VenueRating rating, CancellationToken ct = default)
    {
        _context.VenueRatings.Add(rating);
        await _context.SaveChangesAsync(ct);
    }
}
