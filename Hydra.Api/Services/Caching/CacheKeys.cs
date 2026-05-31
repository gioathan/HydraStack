using System;

namespace Hydra.Api.Caching;

public static class CacheKeys
{
    public const string Ns = "hb";

    public static class Ttl
    {
        public static readonly TimeSpan VenuesList = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan VenueDetail = TimeSpan.FromMinutes(20);
        public static readonly TimeSpan Availability = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan BookingDetail = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan BookingsList = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan VenueTypesList = TimeSpan.FromMinutes(30);
        /// <summary>Short TTL — push token and profile can change.</summary>
        public static readonly TimeSpan CustomerDetail = TimeSpan.FromMinutes(5);
        /// <summary>Rating aggregates change only when a new rating is submitted.</summary>
        public static readonly TimeSpan RatingAggregate = TimeSpan.FromMinutes(10);
        /// <summary>Pending ratings change after bookings end; keep short.</summary>
        public static readonly TimeSpan PendingRatings = TimeSpan.FromMinutes(2);
    }

    public static class Jitter
    {
        public static readonly TimeSpan Venues = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan Availability = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan Bookings = TimeSpan.FromSeconds(20);
        public static readonly TimeSpan VenueTypes = TimeSpan.FromSeconds(60);
        public static readonly TimeSpan Customers = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan Ratings = TimeSpan.FromSeconds(15);
    }

    // ==========================================
    // VERSION TOKENS
    // ==========================================

    public static string VenuesToken => $"{Ns}:venues:ver";
    public static string AvailabilityToken => $"{Ns}:availability:ver";
    public static string BookingsToken => $"{Ns}:bookings:ver";
    public static string VenueTypesToken => $"{Ns}:venue-types:ver";
    public static string CustomersToken => $"{Ns}:customers:ver";
    public static string RatingsToken => $"{Ns}:ratings:ver";

    // ==========================================
    // VENUE CACHE KEYS
    // ==========================================

    public static string VenuesList(int page, int pageSize, Guid? venueTypeId, int version, string? name = null, string? location = null)
        => $"{Ns}:venues:list:v{version}:p{page}:s{pageSize}:t{venueTypeId}:n{name?.ToLowerInvariant()}:l{location?.ToLowerInvariant()}";

    public static string LocationsList(int version) => $"{Ns}:venues:locations:v{version}";

    public static string VenueDetail(Guid id, int version) => $"{Ns}:venues:v{version}:{id}";

    // ==========================================
    // VENUE TYPE CACHE KEYS
    // ==========================================

    public static string VenueTypesList(int page, int pageSize, int version)
        => $"{Ns}:venue-types:list:v{version}:p{page}:s{pageSize}";

    public static string VenueTypeDetail(Guid id, int version)
        => $"{Ns}:venue-types:v{version}:{id}";

    // ==========================================
    // AVAILABILITY CACHE KEYS
    // ==========================================

    public static string Availability(Guid venueId, DateOnly date, int partySize, int version)
        => $"{Ns}:availability:v{version}:{venueId}:{date:yyyy-MM-dd}:p{partySize}";

    // ==========================================
    // BOOKING CACHE KEYS
    // ==========================================

    public static string BookingDetail(Guid id, int version) => $"{Ns}:bookings:v{version}:{id}";

    public static string BookingsList(Guid? venueId, Guid? customerId, string? status, int page, int pageSize, int version)
    {
        var parts = new List<string> { Ns, "bookings", $"v{version}", $"p{page}", $"s{pageSize}" };
        if (venueId.HasValue) parts.Add($"venue:{venueId}");
        if (customerId.HasValue) parts.Add($"customer:{customerId}");
        if (!string.IsNullOrWhiteSpace(status)) parts.Add($"status:{status.ToLower()}");
        return string.Join(":", parts);
    }

    // ==========================================
    // CUSTOMER CACHE KEYS
    // ==========================================

    public static string CustomerDetail(Guid id, int version) => $"{Ns}:customers:v{version}:{id}";

    // ==========================================
    // RATING CACHE KEYS
    // ==========================================

    /// <summary>Per-venue rating aggregate, independently versioned from venue data.</summary>
    public static string RatingAggregate(Guid venueId, int version)
        => $"{Ns}:ratings:aggregate:v{version}:{venueId}";

    /// <summary>Pending ratings list for a customer.</summary>
    public static string PendingRatings(Guid customerId, int version)
        => $"{Ns}:ratings:pending:v{version}:{customerId}";
}
