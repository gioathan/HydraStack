using Hydra.Api.Models;

namespace Hydra.Api.Repositories.VenuePricing;

public interface IVenuePricingRepository
{
    Task ReplaceAllAsync(Guid venueId, IReadOnlyList<VenuePricingItem> items, CancellationToken ct = default);
}
