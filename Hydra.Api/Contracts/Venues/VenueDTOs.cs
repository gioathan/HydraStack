namespace Hydra.Api.Contracts.Venues;

public record VenuePhotoDto(Guid Id, string? Url, int DisplayOrder);

public record VenueEventPhotoDto(Guid Id, string Url, int DisplayOrder);

public record VenueEventDto(
    Guid Id,
    Guid VenueId,
    string Title,
    string? Description,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc,
    DateTime? ClosedAtUtc,
    string? MainPhotoUrl,
    IReadOnlyList<VenueEventPhotoDto> AdditionalPhotos,
    bool IsPast);

public record EventListItemDto(
    Guid Id,
    Guid VenueId,
    string VenueName,
    string? VenueLocation,
    string Title,
    string? Description,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc,
    string? MainPhotoUrl);

public record CreateVenueEventRequest(
    string Title,
    DateTime StartsAtUtc,
    string? Description = null,
    DateTime? EndsAtUtc = null,
    string? MainPhotoUrl = null);

public record UpdateVenueEventRequest(
    string Title,
    DateTime StartsAtUtc,
    string? Description = null,
    DateTime? EndsAtUtc = null,
    string? MainPhotoUrl = null);

public record AddEventPhotoRequest(string Url, int DisplayOrder);

public record ToggleBookingsRequest(bool Enabled);

public record ToggleEventsRequest(bool Enabled);

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
    string? GoogleMapsUrl,
    bool BookingsEnabled,
    bool EventsEnabled,
    int? OpenHour,
    int? CloseHour);

public record CreateVenueRequest(
    string Name,
    string Address,
    int Capacity,
    Guid VenueTypeId,
    Guid UserId,
    string? Description = null,
    string? Location = null);

public record UpdateVenueRequest(
    string Name,
    string Address,
    int Capacity,
    Guid VenueTypeId,
    string? Description = null,
    string? MapsUrl = null);

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

public record AddVenuePhotoRequest(string Url, int DisplayOrder);

public record ReorderVenuePhotosRequest(IReadOnlyList<PhotoOrderItem> Items);

public record PhotoOrderItem(Guid PhotoId, int DisplayOrder);
