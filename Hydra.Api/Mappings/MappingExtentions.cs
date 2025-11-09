using Hydra.Api.Models;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Contracts.Customers;
using Hydra.Api.Contracts.Bookings;
using Hydra.Api.Contracts.Users;
using Hydra.Api.Contracts.VenueTypes;

namespace Hydra.Api.Mapping;

public static class MappingExtensions
{
    public static VenueDto ToDto(this Venue venue) =>
        new(
            venue.Id,
            venue.Name,
            venue.Address,
            venue.Capacity,
            venue.UserId
        );

    public static Venue ToModel(this CreateVenueRequest request) =>
        new()
        {
            Name = request.Name,
            Address = request.Address,
            Capacity = request.Capacity,
            VenueTypeId = request.VenueTypeId,
            UserId = request.UserId
        };

    public static void UpdateFrom(this Venue venue, UpdateVenueRequest request)
    {
        venue.Name = request.Name;
        venue.Address = request.Address;
        venue.Capacity = request.Capacity;
        venue.VenueTypeId = request.VenueTypeId;
    }

    public static CustomerDto ToDto(this Customer customer) =>
        new(
            customer.Id,
            customer.Email,
            customer.Phone,
            customer.Locale,
            customer.MarketingOptIn,
            customer.CreatedAtUtc,
            customer.Name
        );

    public static Customer ToModel(this CreateCustomerRequest request) =>
        new()
        {
            Email = request.Email,
            Phone = request.Phone,
            Locale = request.Locale,
            MarketingOptIn = request.MarketingOptIn,
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
        if (booking.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException($"Can only cancel confirmed bookings. Current status: {booking.Status}");

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAtUtc = DateTime.UtcNow;
    }

    public static UserDto ToDto(this User user) =>
        new(
            user.Id,
            user.Email,
            user.Role.ToString()
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
}