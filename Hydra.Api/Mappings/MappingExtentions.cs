using Hydra.Api.Models;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Contracts.Customers;
using Hydra.Api.Contracts.Bookings;
using Hydra.Api.Contracts.Users;

namespace Hydra.Api.Mapping;

public static class MappingExtensions
{
    // ==========================================
    // VENUE MAPPINGS
    // ==========================================

    /// <summary>
    /// Model → DTO
    /// </summary>
    public static VenueDto ToDto(this Venue venue) =>
        new(
            venue.Id,
            venue.Name,
            venue.Address,
            venue.Capacity
        );

    /// <summary>
    /// DTO → Model (Create)
    /// </summary>
    public static Venue ToModel(this CreateVenueRequest request) =>
        new()
        {
            Name = request.Name,
            Address = request.Address,
            Capacity = request.Capacity
            // NOTE: Venue model does not have CreatedAtUtc in your code
        };

    /// <summary>
    /// DTO → Model (Update)
    /// </summary>
    public static void UpdateFrom(this Venue venue, UpdateVenueRequest request)
    {
        venue.Name = request.Name;
        venue.Address = request.Address;
        venue.Capacity = request.Capacity;
    }

    // ==========================================
    // CUSTOMER MAPPINGS
    // ==========================================

    /// <summary>
    /// Model → DTO
    /// </summary>
    public static CustomerDto ToDto(this Customer customer) =>
        new(
            customer.Id,
            customer.Email,
            customer.Phone,
            customer.Locale,
            customer.MarketingOptIn,
            customer.CreatedAtUtc,
            customer.Name // your model DOES have Name
        );

    /// <summary>
    /// DTO → Model (Create)
    /// </summary>
    public static Customer ToModel(this CreateCustomerRequest request) =>
        new()
        {
            Email = request.Email,
            Phone = request.Phone,
            Locale = request.Locale,
            MarketingOptIn = request.MarketingOptIn,
            CreatedAtUtc = DateTime.UtcNow,
            Name = request.Name
        };

    // ==========================================
    // BOOKING MAPPINGS
    // ==========================================

    /// <summary>
    /// Model → DTO
    /// </summary>
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

    /// <summary>
    /// DTO → Model (Create)
    /// </summary>
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

    // ==========================================
    // BOOKING STATE HELPERS (fit current model)
    // ==========================================

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

    /// <summary>Model → DTO</summary>
    public static UserDto ToDto(this User user) =>
        new(
            user.Id,
            user.Email,
            user.Role.ToString()
        );

    /// <summary>DTO → Model (Create)</summary>
    public static User ToModel(this CreateUserRequest request) =>
        new()
        {
            Email = request.Email,
            PasswordHash = "" ,
            Role = Enum.Parse<UserRole>(request.Role, ignoreCase: true)
        };
}
