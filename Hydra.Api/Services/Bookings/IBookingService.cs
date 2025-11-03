using Hydra.Api.Contracts.Bookings;

namespace Hydra.Api.Services.Bookings;

public interface IBookingService
{
    Task<List<BookingDto>> GetAllBookingsAsync(Guid? venueId = null, Guid? customerId = null, string? status = null, CancellationToken ct = default);
    Task<BookingDto?> GetBookingByIdAsync(Guid id, CancellationToken ct = default);
    Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct = default);
    Task<BookingDto?> ConfirmBookingAsync(Guid id, BookingDecisionRequest request, CancellationToken ct = default);
    Task<BookingDto?> DeclineBookingAsync(Guid id, BookingDecisionRequest request, CancellationToken ct = default);
    Task<BookingDto?> CancelBookingAsync(Guid id, CancelBookingRequest request, CancellationToken ct = default);
    Task<AvailabilityDto> CheckAvailabilityAsync(Guid venueId, DateOnly date, int partySize, CancellationToken ct = default);
}