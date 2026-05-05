using Hydra.Api.Contracts.Bookings;
using Hydra.Api.Contracts.Common;

namespace Hydra.Api.Services.Bookings;

public interface IBookingService
{
    Task<PagedResult<BookingDto>> GetAllBookingsAsync(Guid? venueId, Guid? customerId, string? status, int page, int pageSize, CancellationToken ct);
    Task<PagedResult<BookingDto>> GetBookingsForAdminAsync(Guid adminUserId, Guid? venueId, string? status, int page, int pageSize, CancellationToken ct);
    Task<BookingDto?> GetBookingByIdAsync(Guid id, CancellationToken ct);
    Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct);
    Task<BookingDto?> ConfirmBookingAsync(Guid id, BookingDecisionRequest request, CancellationToken ct);
    Task<BookingDto?> DeclineBookingAsync(Guid id, BookingDecisionRequest request, CancellationToken ct);
    Task<BookingDto?> CancelBookingAsync(Guid id, CancelBookingRequest request, string cancelledBy, CancellationToken ct);
    Task<AvailabilityDto> CheckAvailabilityAsync(Guid venueId, DateOnly date, int partySize, CancellationToken ct);
}