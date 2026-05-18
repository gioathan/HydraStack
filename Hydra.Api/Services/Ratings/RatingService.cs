using Hydra.Api.Caching;
using Hydra.Api.Contracts.Ratings;
using Hydra.Api.Models;
using Hydra.Api.Repositories.Bookings;
using Hydra.Api.Repositories.Ratings;

namespace Hydra.Api.Services.Ratings;

public class RatingService : IRatingService
{
    private readonly IRatingRepository _ratingRepo;
    private readonly IBookingRepository _bookingRepo;
    private readonly ICache _cache;

    public RatingService(IRatingRepository ratingRepo, IBookingRepository bookingRepo, ICache cache)
    {
        _ratingRepo = ratingRepo;
        _bookingRepo = bookingRepo;
        _cache = cache;
    }

    public async Task<(bool Success, string? Error)> SubmitRatingAsync(
        Guid venueId,
        Guid customerId,
        SubmitRatingRequest request,
        CancellationToken ct = default)
    {
        if (request.Value < 0m || request.Value > 5m || request.Value % 0.5m != 0m)
            return (false, "Rating must be between 0 and 5 in 0.5 increments.");

        var booking = await _bookingRepo.GetByIdAsync(request.BookingId, ct);

        if (booking is null || booking.CustomerId != customerId || booking.VenueId != venueId)
            return (false, "Booking not found.");

        if (booking.Status != BookingStatus.Confirmed)
            return (false, "You can only rate venues from confirmed bookings.");

        if (booking.EndUtc > DateTime.UtcNow)
            return (false, "You can only rate a venue after your visit has ended.");

        if (await _ratingRepo.HasRatedAsync(customerId, venueId, ct))
            return (false, "You have already rated this venue.");

        await _ratingRepo.AddAsync(new VenueRating
        {
            VenueId = venueId,
            CustomerId = customerId,
            BookingId = request.BookingId,
            Value = request.Value,
            CreatedAtUtc = DateTime.UtcNow
        }, ct);

        await _cache.BumpTokenAsync(CacheKeys.VenuesToken, ct);

        return (true, null);
    }

    public async Task<IReadOnlyList<PendingRatingDto>> GetPendingRatingsAsync(
        Guid customerId,
        CancellationToken ct = default)
    {
        return await _ratingRepo.GetPendingRatingsAsync(customerId, ct);
    }
}
