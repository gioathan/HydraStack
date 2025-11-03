using Hydra.Api.Caching;
using Hydra.Api.Contracts.Bookings;
using Hydra.Api.Mapping;
using Hydra.Api.Models;
using Hydra.Api.Repositories.Bookings;
using Hydra.Api.Repositories.Venues;
using Hydra.Api.Repositories.Customers;

namespace Hydra.Api.Services.Bookings;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepo;
    private readonly IVenueRepository _venueRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ICache _cache;

    public BookingService(
        IBookingRepository bookingRepo,
        IVenueRepository venueRepo,
        ICustomerRepository customerRepo,
        ICache cache)
    {
        _bookingRepo = bookingRepo;
        _venueRepo = venueRepo;
        _customerRepo = customerRepo;
        _cache = cache;
    }

    public async Task<List<BookingDto>> GetAllBookingsAsync(
        Guid? venueId = null,
        Guid? customerId = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var version = await _cache.GetTokenAsync(CacheKeys.BookingsToken, ct: ct);
        var key = CacheKeys.BookingsList(venueId, customerId, version);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.BookingsList,
            factory: async ct =>
            {
                var bookings = await _bookingRepo.GetAllAsync(venueId, customerId, status, ct);
                return bookings.Select(b => b.ToDto()).ToList();
            },
            jitter: CacheKeys.Jitter.Bookings,
            ct: ct
        );
    }

    public async Task<BookingDto?> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
    {
        var version = await _cache.GetTokenAsync(CacheKeys.BookingsToken, ct: ct);
        var key = CacheKeys.BookingDetail(id, version);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.BookingDetail,
            factory: async ct =>
            {
                var booking = await _bookingRepo.GetByIdAsync(id, ct);
                return booking?.ToDto();
            },
            cacheNull: true,
            jitter: CacheKeys.Jitter.Bookings,
            ct: ct
        );
    }

    public async Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        if (request.StartUtc >= request.EndUtc)
        {
            throw new InvalidOperationException("Start time must be before end time");
        }

        if (request.PartySize <= 0)
        {
            throw new InvalidOperationException("Party size must be greater than 0");
        }

        var venue = await _venueRepo.GetByIdWithRulesAsync(request.VenueId, ct);
        if (venue is null)
        {
            throw new InvalidOperationException($"Venue with ID {request.VenueId} not found");
        }

        var customer = await _customerRepo.GetByIdAsync(request.CustomerId, ct);
        if (customer is null)
        {
            throw new InvalidOperationException($"Customer with ID {request.CustomerId} not found");
        }

        if (request.PartySize > venue.Capacity)
        {
            throw new InvalidOperationException($"Party size ({request.PartySize}) exceeds venue capacity ({venue.Capacity})");
        }

        var conflictingBookings = await _bookingRepo.GetConflictingBookingsAsync(
            request.VenueId,
            request.StartUtc,
            request.EndUtc,
            ct);

        if (conflictingBookings.Any())
        {
            throw new InvalidOperationException("The requested time slot conflicts with an existing booking");
        }

        var booking = request.ToModel();

        if (venue.Rules?.AutoConfirm == true)
        {
            booking.Status = BookingStatus.Confirmed;
        }

        var created = await _bookingRepo.AddAsync(booking, ct);

        await _cache.BumpTokenAsync(CacheKeys.BookingsToken, ct);
        await _cache.BumpTokenAsync(CacheKeys.AvailabilityToken, ct);

        return created.ToDto();
    }

    public async Task<BookingDto?> ConfirmBookingAsync(Guid id, BookingDecisionRequest request, CancellationToken ct = default)
    {
        var booking = await _bookingRepo.GetByIdAsync(id, ct);
        if (booking is null)
            return null;

        booking.Confirm();
        await _bookingRepo.UpdateAsync(booking, ct);

        await _cache.BumpTokenAsync(CacheKeys.BookingsToken, ct);
        await _cache.BumpTokenAsync(CacheKeys.AvailabilityToken, ct);

        return booking.ToDto();
    }

    public async Task<BookingDto?> DeclineBookingAsync(Guid id, BookingDecisionRequest request, CancellationToken ct = default)
    {
        var booking = await _bookingRepo.GetByIdAsync(id, ct);
        if (booking is null)
            return null;

        booking.Decline();
        await _bookingRepo.UpdateAsync(booking, ct);

        await _cache.BumpTokenAsync(CacheKeys.BookingsToken, ct);
        await _cache.BumpTokenAsync(CacheKeys.AvailabilityToken, ct);

        return booking.ToDto();
    }

    public async Task<BookingDto?> CancelBookingAsync(Guid id, CancelBookingRequest request, CancellationToken ct = default)
    {
        var booking = await _bookingRepo.GetByIdAsync(id, ct);
        if (booking is null)
            return null;

        booking.Cancel();
        await _bookingRepo.UpdateAsync(booking, ct);

        await _cache.BumpTokenAsync(CacheKeys.BookingsToken, ct);
        await _cache.BumpTokenAsync(CacheKeys.AvailabilityToken, ct);

        return booking.ToDto();
    }

    public async Task<AvailabilityDto> CheckAvailabilityAsync(
        Guid venueId,
        DateOnly date,
        int partySize,
        CancellationToken ct = default)
    {
        var version = await _cache.GetTokenAsync(CacheKeys.AvailabilityToken, ct: ct);
        var key = CacheKeys.Availability(venueId, date, partySize, version);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.Availability,
            factory: async ct =>
            {
                var venue = await _venueRepo.GetByIdWithRulesAsync(venueId, ct);
                if (venue is null)
                {
                    return new AvailabilityDto(
                        venueId,
                        date,
                        partySize,
                        false,
                        "Venue not found",
                        new List<TimeSlot>());
                }

                if (partySize > venue.Capacity)
                {
                    return new AvailabilityDto(
                        venueId,
                        date,
                        partySize,
                        false,
                        $"Party size ({partySize}) exceeds venue capacity ({venue.Capacity})",
                        new List<TimeSlot>());
                }

                var existingBookings = await _bookingRepo.GetBookingsByVenueAndDateAsync(venueId, date, ct);

                var slotMinutes = venue.Rules?.SlotMinutes ?? 90;
                var availableSlots = GenerateAvailableSlots(date, existingBookings, slotMinutes);

                var isAvailable = availableSlots.Any();
                var reason = isAvailable
                    ? $"{availableSlots.Count} slot(s) available"
                    : "No available slots for this date";

                return new AvailabilityDto(
                    venueId,
                    date,
                    partySize,
                    isAvailable,
                    reason,
                    availableSlots);
            },
            jitter: CacheKeys.Jitter.Availability,
            ct: ct
        );
    }

    private List<TimeSlot> GenerateAvailableSlots(
        DateOnly date,
        List<Booking> existingBookings,
        int slotMinutes)
    {
        var availableSlots = new List<TimeSlot>();
        var businessStart = date.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);
        var businessEnd = date.ToDateTime(new TimeOnly(22, 0), DateTimeKind.Utc);

        var currentTime = businessStart;

        while (currentTime.AddMinutes(slotMinutes) <= businessEnd)
        {
            var slotEnd = currentTime.AddMinutes(slotMinutes);

            var hasConflict = existingBookings.Any(b =>
                b.StartUtc < slotEnd && b.EndUtc > currentTime);

            if (!hasConflict)
            {
                availableSlots.Add(new TimeSlot(currentTime, slotEnd));
            }

            currentTime = currentTime.AddMinutes(30);
        }

        return availableSlots;
    }
}