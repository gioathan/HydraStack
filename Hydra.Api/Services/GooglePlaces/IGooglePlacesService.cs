namespace Hydra.Api.Services.GooglePlaces;

public interface IGooglePlacesService
{
    /// Returns a direct photo URL for the given GooglePlaceId,
    /// or null if the place has no photos or the call fails.
    Task<string?> GetPhotoUrlAsync(string googlePlaceId,
        int maxWidth = 800, CancellationToken ct = default);
}
