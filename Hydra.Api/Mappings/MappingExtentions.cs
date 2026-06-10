using Hydra.Api.Models;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Contracts.Customers;
using Hydra.Api.Contracts.Bookings;
using Hydra.Api.Contracts.Users;
using Hydra.Api.Contracts.VenueTypes;

namespace Hydra.Api.Mapping;

public static class MappingExtensions
{
    public static VenuePhotoDto ToDto(this VenuePhoto photo, string? photoUrl = null) =>
        new(photo.Id, photo.GooglePlaceId, photo.DisplayOrder, photoUrl);

    public static VenuePricingItemDto ToDto(this VenuePricingItem item) =>
        new(item.Id, item.Category, item.Title, item.Subtitle, item.Price, item.DisplayOrder);

    public static VenueEventPhotoDto ToDto(this VenueEventPhoto photo) =>
        new(photo.Id, photo.Url, photo.DisplayOrder);

    public static VenueEventDto ToDto(this VenueEvent e) =>
        new(
            e.Id,
            e.VenueId,
            e.Title,
            e.Description,
            e.StartsAtUtc,
            e.EndsAtUtc,
            e.ClosedAtUtc,
            e.MainPhotoUrl,
            e.AdditionalPhotos
                .OrderBy(p => p.DisplayOrder)
                .Select(p => p.ToDto())
                .ToList(),
            e.IsPast);

    public static VenueDto ToDto(
        this Venue venue,
        IReadOnlyList<VenuePhotoDto>? resolvedPhotos = null,
        decimal averageRating = 0m,
        int ratingCount = 0) =>
        new(
            venue.Id,
            venue.Name,
            venue.Address,
            venue.Description,
            venue.Capacity,
            venue.UserId,
            venue.VenueTypeId,
            resolvedPhotos ?? venue.Photos
                .OrderBy(p => p.DisplayOrder)
                .Select(p => p.ToDto())
                .ToList(),
            venue.PricingItems
                .OrderBy(pi => pi.DisplayOrder)
                .Select(pi => pi.ToDto())
                .ToList(),
            averageRating,
            ratingCount,
            venue.Location,
            venue.Latitude,
            venue.Longitude,
            venue.Latitude.HasValue && venue.Longitude.HasValue
                ? $"https://maps.google.com/?q={venue.Latitude},{venue.Longitude}"
                : null,
            venue.BookingsEnabled,
            venue.EventsEnabled);

    public static Venue ToModel(this CreateVenueRequest request) =>
        new()
        {
            Name = request.Name,
            Address = request.Address,
            Description = request.Description,
            Capacity = request.Capacity,
            VenueTypeId = request.VenueTypeId,
            UserId = request.UserId
        };

    public static void UpdateFrom(this Venue venue, UpdateVenueRequest request)
    {
        venue.Name = request.Name;
        venue.Address = request.Address;
        venue.Description = request.Description;
        venue.Capacity = request.Capacity;
        venue.VenueTypeId = request.VenueTypeId;
        venue.Location = request.Location;
        venue.Latitude = request.Latitude;
        venue.Longitude = request.Longitude;
    }

    public static CustomerDto ToDto(this Customer customer) =>
        new(
            customer.Id,
            customer.Email,
            customer.Phone,
            customer.Locale,
            customer.CreatedAtUtc,
            customer.Name,
            customer.PushToken);

    public static Customer ToModel(this CreateCustomerRequest request) =>
        new()
        {
            Email = request.Email,
            Phone = request.Phone,
            Locale = request.Locale,
            CreatedAtUtc = DateTime.UtcNow,
            Name = request.Name,
            UserId = request.UserId
        };

    public static BookingDto ToDto(this Booking booking) =>
        new(
            booking.Id,
            booking.VenueId,
            booking.CustomerId,
            booking.StartUtc,
            booking.EndUtc,
            booking.PartySize,
            booking.Status.ToString(),
            booking.VenueComment,
            booking.CreatedAtUtc,
            booking.UpdatedAtUtc
        );

    public static Booking ToModel(this CreateBookingRequest request) =>
        new()
        {
            VenueId = request.VenueId,
            CustomerId = request.CustomerId,
            StartUtc = request.StartUtc,
            EndUtc = request.EndUtc,
            PartySize = request.PartySize,
            Status = BookingStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    public static void Confirm(this Booking booking)
    {
        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm booking in {booking.Status} status.");

        booking.Status = BookingStatus.Confirmed;
        booking.UpdatedAtUtc = DateTime.UtcNow;
    }

    public static void Decline(this Booking booking)
    {
        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Cannot decline booking in {booking.Status} status.");

        booking.Status = BookingStatus.Declined;
        booking.UpdatedAtUtc = DateTime.UtcNow;
    }

    public static void Cancel(this Booking booking)
    {
        if (booking.Status != BookingStatus.Confirmed && booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Cannot cancel a booking in {booking.Status} status.");

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAtUtc = DateTime.UtcNow;
    }

    public static UserDto ToDto(this User user) =>
        new(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.IsEmailVerified
        );

    public static User ToModel(this CreateUserRequest request)
    {
        var role = UserRole.Customer; // default
        if (!string.IsNullOrWhiteSpace(request.Role) &&
            Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var parsed))
        {
            role = parsed;
        }

        return new User
        {
            Email = request.Email,
            PasswordHash = "",
            Role = role
        };
    }

    public static VenueTypeDto ToDto(this VenueType venueType) =>
        new(
            venueType.Id,
            venueType.Name,
            venueType.Description,
            venueType.DisplayOrder
        );

    public static VenueType ToModel(this CreateVenueTypeRequest request) =>
        new()
        {
            Name = request.Name,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder
        };

    public static void UpdateFrom(this VenueType venueType, UpdateVenueTypeRequest request)
    {
        venueType.Name = request.Name;
        venueType.Description = request.Description;
        venueType.DisplayOrder = request.DisplayOrder;
    }

    public static void UpdateFrom(this Customer customer, UpdateCustomerRequest request)
    {
        customer.Name = request.Name;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Locale = request.Locale;
    }
}