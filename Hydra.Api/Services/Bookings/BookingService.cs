using Hydra.Api.Caching;
using Hydra.Api.Contracts.Bookings;
using Hydra.Api.Contracts.Common;
using Hydra.Api.Data;
using Hydra.Api.Mapping;
using Hydra.Api.Models;
using Hydra.Api.Repositories.Bookings;
using Hydra.Api.Repositories.Venues;
using Hydra.Api.Repositories.Customers;
using Hydra.Api.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Services.Bookings;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepo;
    private readonly IVenueRepository _venueRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ICache _cache;
    private readonly AppDbContext _context;
    private readonly INotificationQueue _notificationQueue;

    public BookingService(
        IBookingRepository bookingRepo,
        IVenueRepository venueRepo,
        ICustomerRepository customerRepo,
        ICache cache,
        AppDbContext context,
        INotificationQueue notificationQueue)
    {
        _bookingRepo = bookingRepo;
        _venueRepo = venueRepo;
        _customerRepo = customerRepo;
        _cache = cache;
        _context = context;
        _notificationQueue = notificationQueue;
    }

    public async Task<PagedResult<BookingDto>> GetAllBookingsAsync(
        Guid? venueId,
        Guid? customerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var safeSize = Math.Clamp(pageSize, 1, 100);
        var skip = (Math.Max(1, page) - 1) * safeSize;
        var version = await _cache.GetTokenAsync(CacheKeys.BookingsToken, ct: ct);
        var key = CacheKeys.BookingsList(venueId, customerId, status, page, safeSize, version);

        return await _cache.GetOrSetAsync(
            key: key,
            ttl: CacheKeys.Ttl.BookingsList,
            factory: async ct =>
            {
                var (items, total) = await _bookingRepo.GetAllAsync(venueId, customerId, status, skip, safeSize, ct);
                return new PagedResult<BookingDto>(items.Select(b => b.ToDto()).ToList(), total, page, safeSize);
            },
            jitter: CacheKeys.Jitter.Bookings,
            ct: ct
        );
    }

    public async Task<PagedResult<BookingDto>> GetBookingsForAdminAsync(
        Guid adminUserId,
        Guid? venueId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var safeSize = Math.Clamp(pageSize, 1, 100);
        var skip = (Math.Max(1, page) - 1) * safeSize;
        var (items, total) = await _bookingRepo.GetBookingsByAdminUserIdAsync(adminUserId, venueId, status, skip, safeSize, ct);
        return new PagedResult<BookingDto>(items.Select(b => b.ToDto()).ToList(), total, page, safeSize);
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
            throw new InvalidOperationException("Start time must be before end time");

        var now = DateTime.UtcNow;
        if (request.StartUtc < now)
            throw new InvalidOperationException("Cannot create a booking in the past.");

        if (request.StartUtc > now.AddMonths(6))
            throw new InvalidOperationException("Bookings cannot be made more than 6 months in advance.");

        var duration = request.EndUtc - request.StartUtc;
        if (duration.TotalMinutes < 15)
            throw new InvalidOperationException("Minimum booking duration is 15 minutes.");

        if (duration.TotalHours > 12)
            throw new InvalidOperationException("Maximum booking duration is 12 hours.");

        if (request.PartySize <= 0)
            throw new InvalidOperationException("Party size must be greater than 0");

        var venue = await _venueRepo.GetByIdWithRulesAsync(request.VenueId, ct);
        if (venue is null)
            throw new InvalidOperationException($"Venue with ID {request.VenueId} not found");

        if (!venue.BookingsEnabled)
            throw new InvalidOperationException("This venue does not accept bookings.");

        var customer = await _customerRepo.GetByIdAsync(request.CustomerId, ct);
        if (customer is null)
            throw new InvalidOperationException($"Customer with ID {request.CustomerId} not found");

        if (request.PartySize > venue.Capacity)
            throw new InvalidOperationException($"Party size ({request.PartySize}) exceeds venue capacity ({venue.Capacity})");

        await using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        try
        {
            var conflicting = await _bookingRepo.GetConflictingBookingsAsync(
                request.VenueId,
                request.StartUtc,
                request.EndUtc,
                ct);

            if (conflicting.Any(b => b.CustomerId == request.CustomerId))
                throw new InvalidOperationException("You already have a booking at this venue during this time.");

            var bookedCapacity = conflicting.Sum(b => b.PartySize);
            if (bookedCapacity + request.PartySize > venue.Capacity)
                throw new InvalidOperationException("This time slot is fully booked. Please choose another time.");

            var booking = request.ToModel();

            if (venue.Rules?.AutoConfirm == true)
                booking.Status = BookingStatus.Confirmed;

            var created = await _bookingRepo.AddAsync(booking, ct);
            created.Customer = customer;

            await transaction.CommitAsync(ct);

            await _cache.BumpTokenAsync(CacheKeys.BookingsToken, ct);
            await _cache.BumpTokenAsync(CacheKeys.AvailabilityToken, ct);

            _notificationQueue.Enqueue(new BookingNotification(
                Type: NotificationType.BookingReceived,
                VenueAdminEmail: venue.User?.Email ?? "",
                VenueName: venue.Name,
                CustomerEmail: customer.Email ?? "",
                CustomerName: customer.Name ?? "Guest",
                CustomerPushToken: customer.PushToken,
                BookingId: created.Id,
                StartUtc: created.StartUtc,
                PartySize: created.PartySize));

            return created.ToDto();
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<BookingDto?> ConfirmBookingAsync(Guid id, BookingDecisionRequest request, CancellationToken ct = default)
    {
        var booking = await _bookingRepo.GetByIdAsync(id, ct);
        if (booking is null)
            return null;

        booking.Confirm();
        booking.VenueComment = request.Note;
        await _bookingRepo.UpdateAsync(booking, ct);

        await _cache.BumpTokenAsync(CacheKeys.BookingsToken, ct);
        await _cache.BumpTokenAsync(CacheKeys.AvailabilityToken, ct);

        var venue = await _venueRepo.GetByIdWithRulesAsync(booking.VenueId, ct);
        var customer = await _customerRepo.GetByIdAsync(booking.CustomerId, ct);

        _notificationQueue.Enqueue(new BookingNotification(
            Type: NotificationType.BookingConfirmed,
            VenueAdminEmail: venue?.User?.Email ?? "",
            VenueName: venue?.Name ?? "",
            CustomerEmail: customer?.Email ?? "",
            CustomerName: customer?.Name ?? "Guest",
            CustomerPushToken: customer?.PushToken,
            BookingId: booking.Id,
            StartUtc: booking.StartUtc,
            PartySize: booking.PartySize));

        return booking.ToDto();
    }

    public async Task<BookingDto?> DeclineBookingAsync(Guid id, BookingDecisionRequest request, CancellationToken ct = default)
    {
        var booking = await _bookingRepo.GetByIdAsync(id, ct);
        if (booking is null)
            return null;

        booking.Decline();
        booking.VenueComment = request.Note;
        await _bookingRepo.UpdateAsync(booking, ct);

        await _cache.BumpTokenAsync(CacheKeys.BookingsToken, ct);
        await _cache.BumpTokenAsync(CacheKeys.AvailabilityToken, ct);

        var venue = await _venueRepo.GetByIdWithRulesAsync(booking.VenueId, ct);
        var customer = await _customerRepo.GetByIdAsync(booking.CustomerId, ct);

        _notificationQueue.Enqueue(new BookingNotification(
            Type: NotificationType.BookingDeclined,
            VenueAdminEmail: venue?.User?.Email ?? "",
            VenueName: venue?.Name ?? "",
            CustomerEmail: customer?.Email ?? "",
            CustomerName: customer?.Name ?? "Guest",
            CustomerPushToken: customer?.PushToken,
            BookingId: booking.Id,
            StartUtc: booking.StartUtc,
            PartySize: booking.PartySize));

        return booking.ToDto();
    }

    public async Task<BookingDto?> CancelBookingAsync(Guid id, CancelBookingRequest request, string cancelledBy, CancellationToken ct = default)
    {
        var booking = await _bookingRepo.GetByIdAsync(id, ct);
        if (booking is null)
            return null;

        booking.Cancel();
        await _bookingRepo.UpdateAsync(booking, ct);

        await _cache.BumpTokenAsync(CacheKeys.BookingsToken, ct);
        await _cache.BumpTokenAsync(CacheKeys.AvailabilityToken, ct);

        var venue = await _venueRepo.GetByIdWithRulesAsync(booking.VenueId, ct);
        var customer = await _customerRepo.GetByIdAsync(booking.CustomerId, ct);

        var notificationType = cancelledBy == "venue"
            ? NotificationType.BookingCancelledByVenue
            : NotificationType.BookingCancelledByCustomer;

        _notificationQueue.Enqueue(new BookingNotification(
            Type: notificationType,
            VenueAdminEmail: venue?.User?.Email ?? "",
            VenueName: venue?.Name ?? "",
            CustomerEmail: customer?.Email ?? "",
            CustomerName: customer?.Name ?? "Guest",
            CustomerPushToken: customer?.PushToken,
            BookingId: booking.Id,
            StartUtc: booking.StartUtc,
            PartySize: booking.PartySize));

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

                if (!venue.BookingsEnabled)
                {
                    return new AvailabilityDto(
                        venueId,
                        date,
                        partySize,
                        false,
                        "This venue does not accept bookings.",
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

                var slotMinutes = venue.Rules?.SlotMinutes ?? 90;
                var openHour = venue.Rules?.OpenHour ?? 9;
                var closeHour = venue.Rules?.CloseHour ?? 22;
                var generatedSlots = GenerateAvailableSlots(date, slotMinutes, openHour, closeHour);

                var existingBookings = await _bookingRepo.GetBookingsByVenueAndDateAsync(venueId, date, ct);

                // A slot stays available as long as the venue has room for this
                // party alongside whoever else already booked it — many different
                // customers can share a slot up to capacity, not just one booking.
                var availableSlots = generatedSlots
                    .Where(slot =>
                    {
                        var bookedCapacity = existingBookings
                            .Where(b => b.StartUtc < slot.EndUtc && b.EndUtc > slot.StartUtc)
                            .Sum(b => b.PartySize);
                        return bookedCapacity + partySize <= venue.Capacity;
                    })
                    .ToList();

                var isAvailable = availableSlots.Any();
                var reason = isAvailable
                    ? $"{availableSlots.Count} slot(s) available"
                    : $"No slots fit within operating hours ({openHour:D2}:00–{closeHour:D2}:00) with a {slotMinutes}-minute duration";

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
        int slotMinutes,
        int openHour = 9,
        int closeHour = 22)
    {
        var availableSlots = new List<TimeSlot>();
        var businessStart = date.ToDateTime(new TimeOnly(openHour, 0), DateTimeKind.Utc);
        var endDate = closeHour < openHour ? date.AddDays(1) : date;
        var businessEnd = endDate.ToDateTime(new TimeOnly(closeHour, 0), DateTimeKind.Utc);

        var currentTime = businessStart;

        while (currentTime.AddMinutes(slotMinutes) <= businessEnd)
        {
            availableSlots.Add(new TimeSlot(currentTime, currentTime.AddMinutes(slotMinutes)));
            currentTime = currentTime.AddMinutes(30);
        }

        return availableSlots;
    }
}
