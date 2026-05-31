using System;

namespace Hydra.Api.Caching;

public static class CacheKeys
{
    public const string Ns = "hb";

    public static class Ttl
    {
        /// <summary>TTL for venue list cache (10 minutes)</summary>
        public static readonly TimeSpan VenuesList = TimeSpan.FromMinutes(10);

        /// <summary>TTL for individual venue details (20 minutes)</summary>
        public static readonly TimeSpan VenueDetail = TimeSpan.FromMinutes(20);

        /// <summary>TTL for availability queries (5 minutes - changes frequently)</summary>
        public static readonly TimeSpan Availability = TimeSpan.FromMinutes(5);

        /// <summary>TTL for booking details (15 minutes)</summary>
        public static readonly TimeSpan BookingDetail = TimeSpan.FromMinutes(15);

        /// <summary>TTL for booking lists (10 minutes)</summary>
        public static readonly TimeSpan BookingsList = TimeSpan.FromMinutes(10);

        /// <summary>
        /// TTL for Google Places photo URLs (23 hours).
        /// photo_reference values are stable for days; 23h avoids serving a stale
        /// reference right as Google rotates it.
        /// </summary>
        public static readonly TimeSpan GooglePlacesPhoto = TimeSpan.FromHours(23);
    }

    public static class Jitter
    {
        /// <summary>Jitter range for venue caches (±30 seconds)</summary>
        public static readonly TimeSpan Venues = TimeSpan.FromSeconds(30);

        /// <summary>Jitter range for availability caches (±10 seconds)</summary>
        public static readonly TimeSpan Availability = TimeSpan.FromSeconds(10);

        /// <summary>Jitter range for booking caches (±20 seconds)</summary>
        public static readonly TimeSpan Bookings = TimeSpan.FromSeconds(20);
    }

    // ==========================================
    // VERSION TOKENS
    // ==========================================

    public static string VenuesToken => $"{Ns}:venues:ver";
    public static string AvailabilityToken => $"{Ns}:availability:ver";
    public static string BookingsToken => $"{Ns}:bookings:ver";

    // ==========================================
    // VENUE CACHE KEYS
    // ==========================================

    public static string VenuesList(int page, int pageSize, Guid? venueTypeId, int version, string? name = null, string? location = null)
        => $"{Ns}:venues:list:v{version}:p{page}:s{pageSize}:t{venueTypeId}:n{name?.ToLowerInvariant()}:l{location?.ToLowerInvariant()}";

    public static string LocationsList(int version) => $"{Ns}:venues:locations:v{version}";

    public static string VenueDetail(Guid id, int version) => $"{Ns}:venues:v{version}:{id}";

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
    // GOOGLE PLACES CACHE KEYS
    // ==========================================

    /// <summary>
    /// Cache key for a resolved Google Places photo URL.
    /// Keyed by place ID and maxWidth so different sizes don't collide.
    /// Example: hb:gplaces:photo:ChIJ...:800
    /// </summary>
    public static string GooglePlacesPhoto(string googlePlaceId, int maxWidth)
        => $"{Ns}:gplaces:photo:{googlePlaceId}:{maxWidth}";
}
