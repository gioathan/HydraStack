using Hydra.Api.Models;

namespace Hydra.Api.Repositories.Bookings;

public interface IBookingRepository
{
    Task<List<Booking>> GetAllAsync(Guid? venueId = null, Guid? customerId = null, string? status = null, CancellationToken ct = default);
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Booking>> GetConflictingBookingsAsync(Guid venueId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    Task<List<Booking>> GetBookingsByVenueAndDateAsync(Guid venueId, DateOnly date, CancellationToken ct = default);
    Task<Booking> AddAsync(Booking booking, CancellationToken ct = default);
    Task UpdateAsync(Booking booking, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}