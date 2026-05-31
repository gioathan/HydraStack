namespace Hydra.Api.Contracts.Venues;

public record VenuePhotoDto(Guid Id, string Url, int DisplayOrder);

public record VenuePricingItemDto(Guid Id, string? Category, string Title, string? Subtitle, decimal Price, int DisplayOrder);

public record PricingItemRequest(string Title, string? Subtitle, decimal Price, int DisplayOrder, string? Category = null);

public record SetVenuePricingRequest(IReadOnlyList<PricingItemRequest> Items);

public record VenueDto(
    Guid Id,
    string Name,
    string Address,
    string? Description,
    int Capacity,
    Guid UserId,
    Guid VenueTypeId,
    IReadOnlyList<VenuePhotoDto> Photos,
    IReadOnlyList<VenuePricingItemDto> PricingItems,
    decimal AverageRating,
    int RatingCount,
    string? Location,
    double? Latitude,
    double? Longitude,
    string? GoogleMapsUrl);

public record CreateVenueRequest(
    string Name,
    string Address,
    int Capacity,
    Guid VenueTypeId,
    Guid UserId,
    string? Description = null);

public record UpdateVenueRequest(
    string Name,
    string Address,
    int Capacity,
    Guid VenueTypeId,
    string? Description = null,
    string? Location = null,
    double? Latitude = null,
    double? Longitude = null);

public record BookingRulesDto(
    bool AutoConfirm,
    int SlotMinutes,
    int OpenHour,
    int CloseHour);

public record UpdateBookingRulesRequest(
    bool AutoConfirm,
    int SlotMinutes,
    int OpenHour,
    int CloseHour);

public record ReorderVenuePhotosRequest(IReadOnlyList<PhotoOrderItem> Items);

public record PhotoOrderItem(Guid PhotoId, int DisplayOrder);
