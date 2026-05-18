using Hydra.Api.Contracts.Ratings;
using Hydra.Api.Models;

namespace Hydra.Api.Repositories.Ratings;

public interface IRatingRepository
{
    Task<bool> HasRatedAsync(Guid customerId, Guid venueId, CancellationToken ct = default);
    Task<(decimal Average, int Count)> GetAggregateAsync(Guid venueId, CancellationToken ct = default);
    Task<Dictionary<Guid, (decimal Average, int Count)>> GetAggregatesAsync(IEnumerable<Guid> venueIds, CancellationToken ct = default);
    Task<List<PendingRatingDto>> GetPendingRatingsAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(VenueRating rating, CancellationToken ct = default);
}
