using Hydra.Api.Data;
using Hydra.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Repositories.Bookings;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Booking>> GetAllAsync(
        Guid? venueId = null,
        Guid? customerId = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var query = _context.Bookings
            .Include(b => b.Venue)
            .Include(b => b.Customer)
            .AsQueryable();

        if (venueId.HasValue)
            query = query.Where(b => b.VenueId == venueId.Value);

        if (customerId.HasValue)
            query = query.Where(b => b.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingStatus>(status, true, out var statusEnum))
            query = query.Where(b => b.Status == statusEnum);

        return await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<List<Booking>> GetBookingsByAdminUserIdAsync(
        Guid adminUserId,
        Guid? venueId,
        string? status,
        CancellationToken ct)
    {
        var query = _context.Bookings
            .Include(b => b.Venue)
            .Include(b => b.Customer)
            .Where(b => b.Venue.UserId == adminUserId);

        if (venueId.HasValue)
            query = query.Where(b => b.VenueId == venueId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingStatus>(status, true, out var statusEnum))
            query = query.Where(b => b.Status == statusEnum);

        return await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Include(b => b.Venue)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<List<Booking>> GetConflictingBookingsAsync(
        Guid venueId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken ct = default)
    {
        return await _context.Bookings
            .Where(b => b.VenueId == venueId)
            .Where(b => b.Status == BookingStatus.Confirmed)
            .Where(b => b.StartUtc < endUtc && b.EndUtc > startUtc)
            .ToListAsync(ct);
    }

    public async Task<List<Booking>> GetBookingsByVenueAndDateAsync(
        Guid venueId,
        DateOnly date,
        CancellationToken ct = default)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await _context.Bookings
            .Where(b => b.VenueId == venueId)
            .Where(b => b.Status == BookingStatus.Confirmed)
            .Where(b => b.StartUtc >= startOfDay && b.StartUtc < endOfDay)
            .OrderBy(b => b.StartUtc)
            .ToListAsync(ct);
    }

    public async Task<Booking> AddAsync(Booking booking, CancellationToken ct = default)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(ct);
        return booking;
    }

    public async Task UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Bookings.AnyAsync(b => b.Id == id, ct);
    }
}