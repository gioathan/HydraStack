namespace Hydra.Api.Contracts.Venues;

public record VenuePhotoDto(Guid Id, string GooglePlaceId, int DisplayOrder, string? PhotoUrl);

public record VenueDto(
    Guid Id,
    string Name,
    string Address,
    string? Description,
    int Capacity,
    Guid UserId,
    Guid VenueTypeId,
    IReadOnlyList<VenuePhotoDto> Photos,
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

public record AddVenuePhotoRequest(string GooglePlaceId, int DisplayOrder);

public record ReorderVenuePhotosRequest(IReadOnlyList<PhotoOrderItem> Items);

public record PhotoOrderItem(Guid PhotoId, int DisplayOrder);
