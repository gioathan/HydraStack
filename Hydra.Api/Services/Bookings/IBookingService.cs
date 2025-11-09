using Hydra.Api.Contracts.Bookings;

namespace Hydra.Api.Services.Bookings;

public interface IBookingService
{
    Task<List<BookingDto>> GetAllBookingsAsync(Guid? venueId, Guid? customerId, string? status, CancellationToken ct);
    Task<List<BookingDto>> GetBookingsForAdminAsync(Guid adminUserId, Guid? venueId, string? status, CancellationToken ct);
    Task<BookingDto?> GetBookingByIdAsync(Guid id, CancellationToken ct);
    Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct);
    Task<BookingDto?> ConfirmBookingAsync(Guid id, BookingDecisionRequest request, CancellationToken ct);
    Task<BookingDto?> DeclineBookingAsync(Guid id, BookingDecisionRequest request, CancellationToken ct);
    Task<BookingDto?> CancelBookingAsync(Guid id, CancelBookingRequest request, CancellationToken ct);
    Task<AvailabilityDto> CheckAvailabilityAsync(Guid venueId, DateOnly date, int partySize, CancellationToken ct);
}