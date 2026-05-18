using Hydra.Api.Contracts.Ratings;

namespace Hydra.Api.Services.Ratings;

public interface IRatingService
{
    Task<(bool Success, string? Error)> SubmitRatingAsync(Guid venueId, Guid customerId, SubmitRatingRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PendingRatingDto>> GetPendingRatingsAsync(Guid customerId, CancellationToken ct = default);
}
