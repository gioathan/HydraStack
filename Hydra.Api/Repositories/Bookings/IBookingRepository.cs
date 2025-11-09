using Hydra.Api.Models;

namespace Hydra.Api.Repositories.Bookings;

public interface IBookingRepository
{
    Task<List<Booking>> GetAllAsync(Guid? venueId, Guid? customerId, string? status, CancellationToken ct);
    Task<List<Booking>> GetBookingsByAdminUserIdAsync(Guid adminUserId, Guid? venueId, string? status, CancellationToken ct);
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Booking>> GetConflictingBookingsAsync(Guid venueId, DateTime startUtc, DateTime endUtc, CancellationToken ct);
    Task<List<Booking>> GetBookingsByVenueAndDateAsync(Guid venueId, DateOnly date, CancellationToken ct);
    Task<Booking> AddAsync(Booking booking, CancellationToken ct);
    Task UpdateAsync(Booking booking, CancellationToken ct);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
}