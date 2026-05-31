using System.Text.Json;
using Hydra.Api.Caching;
using Hydra.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Hydra.Api.Services.GooglePlaces;

public class GooglePlacesService : IGooglePlacesService
{
    private readonly HttpClient _http;
    private readonly GooglePlacesSettings _settings;
    private readonly ICache _cache;
    private readonly ILogger<GooglePlacesService> _logger;

    public GooglePlacesService(
        IHttpClientFactory httpClientFactory,
        IOptions<GooglePlacesSettings> options,
        ICache cache,
        ILogger<GooglePlacesService> logger)
    {
        _http = httpClientFactory.CreateClient("GooglePlaces");
        _settings = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetPhotoUrlAsync(
        string googlePlaceId,
        int maxWidth = 800,
        CancellationToken ct = default)
    {
        // Development: support picsum: prefix for fake photos — never used in production
        if (googlePlaceId.StartsWith("picsum:", StringComparison.OrdinalIgnoreCase))
        {
            var seed = googlePlaceId["picsum:".Length..];
            return $"https://picsum.photos/seed/{seed}/{maxWidth}/600";
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("GooglePlaces:ApiKey is not configured — skipping photo resolution");
            return null;
        }

        var cacheKey = CacheKeys.GooglePlacesPhoto(googlePlaceId, maxWidth);

        return await _cache.GetOrSetAsync(
            key: cacheKey,
            ttl: CacheKeys.Ttl.GooglePlacesPhoto,
            factory: ct => FetchPhotoUrlAsync(googlePlaceId, maxWidth, ct),
            cacheNull: false,
            ct: ct);
    }

    private async Task<string?> FetchPhotoUrlAsync(
        string googlePlaceId,
        int maxWidth,
        CancellationToken ct)
    {
        try
        {
            var detailsUrl =
                $"https://maps.googleapis.com/maps/api/place/details/json" +
                $"?place_id={Uri.EscapeDataString(googlePlaceId)}" +
                $"&fields=photos" +
                $"&key={_settings.ApiKey}";

            using var response = await _http.GetAsync(detailsUrl, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Google Places Details request failed with status {Status} for place {PlaceId}",
                    response.StatusCode, googlePlaceId);
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;

            if (!root.TryGetProperty("result", out var result) ||
                !result.TryGetProperty("photos", out var photos) ||
                photos.GetArrayLength() == 0)
            {
                _logger.LogWarning("No photos found for place {PlaceId}", googlePlaceId);
                return null;
            }

            if (!photos[0].TryGetProperty("photo_reference", out var photoRefEl))
            {
                _logger.LogWarning("photo_reference missing for place {PlaceId}", googlePlaceId);
                return null;
            }

            var photoRef = photoRefEl.GetString();
            if (string.IsNullOrWhiteSpace(photoRef))
                return null;

            return
                $"https://maps.googleapis.com/maps/api/place/photo" +
                $"?maxwidth={maxWidth}" +
                $"&photo_reference={Uri.EscapeDataString(photoRef)}" +
                $"&key={_settings.ApiKey}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve photo URL for place {PlaceId}", googlePlaceId);
            return null;
        }
    }
}
