using System;

namespace Hydra.Api.Caching;

/// <summary>
/// Centralized cache key management for Hydra booking system.
/// All keys are prefixed with a namespace to prevent collisions with other applications.
/// Uses version-based invalidation: when data changes, bump the version token to invalidate all related caches.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Namespace prefix for all cache keys (hydra-booking).
    /// Prevents key collisions if Redis is shared across multiple applications.
    /// </summary>
    public const string Ns = "hb";

    /// <summary>
    /// Cache TTL (Time To Live) configurations.
    /// Defines how long different types of data should be cached.
    /// </summary>
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
    }

    /// <summary>
    /// Jitter configurations to prevent cache stampede.
    /// Jitter adds random variation to expiration times so not all caches expire simultaneously.
    /// </summary>
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

    /// <summary>
    /// Version token for venue-related caches.
    /// Call BumpTokenAsync(VenuesToken) after creating/updating/deleting a venue to invalidate all venue caches.
    /// </summary>
    public static string VenuesToken => $"{Ns}:venues:ver";

    /// <summary>
    /// Version token for availability-related caches.
    /// Call BumpTokenAsync(AvailabilityToken) when availability data changes.
    /// </summary>
    public static string AvailabilityToken => $"{Ns}:availability:ver";

    /// <summary>
    /// Version token for booking-related caches.
    /// Call BumpTokenAsync(BookingsToken) after creating/updating/deleting a booking.
    /// </summary>
    public static string BookingsToken => $"{Ns}:bookings:ver";

    // ==========================================
    // VENUE CACHE KEYS
    // ==========================================

    /// <summary>
    /// Generates cache key for the list of all venues.
    /// Example: "hb:venues:list:v1"
    /// </summary>
    /// <param name="version">Version number from VenuesToken</param>
    /// <returns>Versioned cache key for venue list</returns>
    public static string VenuesList(int version) => $"{Ns}:venues:list:v{version}";

    /// <summary>
    /// Generates cache key for a specific venue's details.
    /// Example: "hb:venues:v1:3fa85f64-5717-4562-b3fc-2c963f66afa6"
    /// </summary>
    /// <param name="id">Venue ID</param>
    /// <param name="version">Version number from VenuesToken</param>
    /// <returns>Versioned cache key for venue detail</returns>
    public static string VenueDetail(Guid id, int version) => $"{Ns}:venues:v{version}:{id}";

    // ==========================================
    // AVAILABILITY CACHE KEYS
    // ==========================================

    /// <summary>
    /// Generates cache key for availability query.
    /// Example: "hb:availability:v1:venue-guid:2025-10-25:p4"
    /// </summary>
    /// <param name="venueId">Venue ID</param>
    /// <param name="date">Date to check availability</param>
    /// <param name="partySize">Number of guests</param>
    /// <param name="version">Version number from AvailabilityToken</param>
    /// <returns>Versioned cache key for availability</returns>
    public static string Availability(Guid venueId, DateOnly date, int partySize, int version)
        => $"{Ns}:availability:v{version}:{venueId}:{date:yyyy-MM-dd}:p{partySize}";

    // ==========================================
    // BOOKING CACHE KEYS
    // ==========================================

    /// <summary>
    /// Generates cache key for a specific booking's details.
    /// Example: "hb:bookings:v1:booking-guid"
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="version">Version number from BookingsToken</param>
    /// <returns>Versioned cache key for booking detail</returns>
    public static string BookingDetail(Guid id, int version) => $"{Ns}:bookings:v{version}:{id}";

    /// <summary>
    /// Generates cache key for filtered booking list.
    /// Example: "hb:bookings:v1:venue:guid" or "hb:bookings:v1:customer:guid"
    /// </summary>
    /// <param name="venueId">Optional venue ID filter</param>
    /// <param name="customerId">Optional customer ID filter</param>
    /// <param name="version">Version number from BookingsToken</param>
    /// <returns>Versioned cache key for booking list with filters</returns>
    public static string BookingsList(Guid? venueId, Guid? customerId, int version)
    {
        var parts = new List<string> { Ns, "bookings", $"v{version}" };

        if (venueId.HasValue)
            parts.Add($"venue:{venueId}");

        if (customerId.HasValue)
            parts.Add($"customer:{customerId}");

        return string.Join(":", parts);
    }
}