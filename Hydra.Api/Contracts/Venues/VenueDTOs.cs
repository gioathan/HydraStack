namespace Hydra.Api.Contracts.Venues;

public record VenuePhotoDto(Guid Id, string GooglePlaceId, int DisplayOrder, string? PhotoUrl);

public record VenueDto(
    Guid Id,
    string Name,
    string Address,
    int Capacity,
    Guid UserId,
    Guid VenueTypeId,
    IReadOnlyList<VenuePhotoDto> Photos,
    decimal AverageRating,
    int RatingCount);

public record CreateVenueRequest(
    string Name,
    string Address,
    int Capacity,
    Guid VenueTypeId,
    Guid UserId);

public record UpdateVenueRequest(
    string Name,
    string Address,
    int Capacity,
    Guid VenueTypeId);

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
