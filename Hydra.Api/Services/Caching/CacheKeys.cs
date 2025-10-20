namespace Hydra.Api.Caching;

public static class CacheKeys
{
    public const string Ns = "hb"; // hydra-booking

    // Group/version tokens (use BumpTokenAsync on writes)
    public static string VenuesToken => $"{Ns}:venues:ver";
    public static string AvailabilityToken => $"{Ns}:availability:ver"; // optional, if you want a group bump

    // Concrete keys (v = token value)
    public static string VenuesList(int v) => $"{Ns}:venues:list:v{v}";
    public static string VenueDetail(Guid id, int v) => $"{Ns}:venues:v{v}:{id}";

    public static string Availability(Guid venueId, DateOnly date, int party, int v)
        => $"{Ns}:availability:v{v}:{venueId}:{date:yyyy-MM-dd}:p{party}";
}
