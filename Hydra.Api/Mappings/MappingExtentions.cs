using Hydra.Api.Models;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Contracts.Customers;
using Hydra.Api.Contracts.Bookings;

namespace Hydra.Api.Mapping;

public static class MappingExtensions
{
    // ==========================================
    // VENUE MAPPINGS
    // ==========================================

    /// <summary>
    /// Convert Venue model to DTO (Model → DTO)
    /// </summary>
    public static VenueDto ToDto(this Venue venue) =>
        new(
            venue.Id,
            venue.Name,
            venue.Address,
            venue.Capacity
        );

    /// <summary>
    /// Convert CreateVenueRequest to Venue model (DTO → Model)
    /// </summary>
    public static Venue ToModel(this CreateVenueRequest request) =>
        new()
        {
            Name = request.Name,
            Address = request.Address,
            Capacity = request.Capacity,
            CreatedAtUtc = DateTime.UtcNow
        };

    /// <summary>
    /// Update existing Venue from UpdateVenueRequest (DTO → Model update)
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
    /// Convert Customer model to DTO (Model → DTO)
    /// </summary>
    public static CustomerDto ToDto(this Customer customer) =>
        new(
            customer.Id,
            customer.Email,
            customer.Phone,
            customer.Locale,
            customer.MarketingOptIn,
            customer.CreatedAtUtc,
            Name: null  // Name field in DTO but not in model - keeping for future use
        );

    /// <summary>
    /// Convert CreateCustomerRequest to Customer model (DTO → Model)
    /// </summary>
    public static Customer ToModel(this CreateCustomerRequest request) =>
        new()
        {
            Email = request.Email,
            Phone = request.Phone,
            Locale = request.Locale,
            MarketingOptIn = request.MarketingOptIn,
            CreatedAtUtc = DateTime.UtcNow
            // Note: Name in request is ignored as Customer model doesn't have a Name property yet
        };

    // ==========================================
    // BOOKING MAPPINGS
    // ==========================================

    /// <summary>
    /// Convert Booking model to DTO (Model → DTO)
    /// </summary>
    public static BookingDto ToDto(this Booking booking) =>
        new(
            booking.Id,
            booking.VenueId,
            booking.CustomerId,
            booking.StartUtc,
            booking.EndUtc,
            booking.PartySize,
            booking.Status.ToString(),  // Convert enum to string for API response
            booking.RequestedAtUtc,
            booking.DecidedAtUtc,
            booking.CustomerNote,
            booking.AdminNote
        );

    /// <summary>
    /// Convert CreateBookingRequest to Booking model (DTO → Model)
    /// </summary>
    public static Booking ToModel(this CreateBookingRequest request) =>
        new()
        {
            VenueId = request.VenueId,
            CustomerId = request.CustomerId,
            StartUtc = request.StartUtc,
            EndUtc = request.EndUtc,
            PartySize = request.PartySize,
            CustomerNote = request.CustomerNote,
            // Server-controlled fields:
            Status = BookingStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    /// <summary>
    /// Apply a booking decision (confirm/decline) to an existing Booking
    /// </summary>
    public static void ApplyDecision(
        this Booking booking,
        BookingDecisionRequest decision,
        bool isConfirmed)
    {
        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot apply decision to booking in {booking.Status} status. Only Pending bookings can be decided.");
        }

        booking.Status = isConfirmed ? BookingStatus.Confirmed : BookingStatus.Declined;
        booking.DecidedAtUtc = DateTime.UtcNow;
        booking.DecidedBy = decision.Admin;
        booking.AdminNote = decision.Note;
        booking.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Confirm a booking
    /// </summary>
    public static void Confirm(this Booking booking, string adminIdentifier, string? note = null)
    {
        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot confirm booking in {booking.Status} status");
        }

        booking.Status = BookingStatus.Confirmed;
        booking.DecidedAtUtc = DateTime.UtcNow;
        booking.DecidedBy = adminIdentifier;
        booking.AdminNote = note;
        booking.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Decline a booking
    /// </summary>
    public static void Decline(this Booking booking, string adminIdentifier, string? note = null)
    {
        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot decline booking in {booking.Status} status");
        }

        booking.Status = BookingStatus.Declined;
        booking.DecidedAtUtc = DateTime.UtcNow;
        booking.DecidedBy = adminIdentifier;
        booking.AdminNote = note;
        booking.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancel a confirmed booking
    /// </summary>
    public static void Cancel(this Booking booking, string? cancelledBy = null)
    {
        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException(
                $"Can only cancel confirmed bookings. Current status: {booking.Status}");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(cancelledBy))
        {
            booking.AdminNote = string.IsNullOrEmpty(booking.AdminNote)
                ? $"Cancelled by: {cancelledBy}"
                : $"{booking.AdminNote}\nCancelled by: {cancelledBy}";
        }
    }

    /// <summary>
    /// Mark a booking as seated (customer arrived)
    /// </summary>
    public static void MarkAsSeated(this Booking booking)
    {
        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException(
                $"Can only seat confirmed bookings. Current status: {booking.Status}");
        }

        booking.Status = BookingStatus.Seated;
        booking.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark a booking as no-show (customer didn't arrive)
    /// </summary>
    public static void MarkAsNoShow(this Booking booking)
    {
        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException(
                $"Can only mark confirmed bookings as no-show. Current status: {booking.Status}");
        }

        booking.Status = BookingStatus.NoShow;
        booking.UpdatedAtUtc = DateTime.UtcNow;
    }
}