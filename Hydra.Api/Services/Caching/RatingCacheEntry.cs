namespace Hydra.Api.Caching;

/// <summary>
/// Serialization-friendly wrapper for a venue's cached rating aggregate.
/// Value tuples don't round-trip cleanly through System.Text.Json, so we use a record.
/// </summary>
public record RatingCacheEntry(decimal Average, int Count);
