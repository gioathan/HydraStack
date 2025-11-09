using Hydra.Api.Contracts.Bookings;
using Hydra.Api.Services.Bookings;
using Hydra.Api.Services.Venues;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IVenueService _venueService;

    public BookingsController(IBookingService bookingService, IVenueService venueService)
    {
        _bookingService = bookingService;
        _venueService = venueService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BookingDto>>> GetBookings(
        [FromQuery] Guid? venueId,
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "SuperAdmin")
        {
            var allBookings = await _bookingService.GetAllBookingsAsync(venueId, customerId, status, ct);
            return Ok(allBookings);
        }

        if (userRole == "Admin")
        {
            if (venueId.HasValue)
            {
                var venue = await _venueService.GetVenueByIdAsync(venueId.Value, ct);
                if (venue?.UserId != currentUserId)
                {
                    return Forbid();
                }
            }
            
            var adminBookings = await _bookingService.GetBookingsForAdminAsync(currentUserId, venueId, status, ct);
            return Ok(adminBookings);
        }

        if (userRole == "Customer")
        {
            var customerBookings = await _bookingService.GetAllBookingsAsync(venueId, currentUserId, status, ct);
            return Ok(customerBookings);
        }

        return Forbid();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> GetBookingById(Guid id, CancellationToken ct)
    {
        var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var booking = await _bookingService.GetBookingByIdAsync(id, ct);

        if (booking is null)
            return NotFound(new { message = $"Booking with ID {id} not found" });

        if (userRole == "SuperAdmin")
        {
            return Ok(booking);
        }

        if (userRole == "Admin")
        {
            var venue = await _venueService.GetVenueByIdAsync(booking.VenueId, ct);
            if (venue?.UserId != currentUserId)
            {
                return Forbid();
            }
            return Ok(booking);
        }

        if (userRole == "Customer" && booking.CustomerId == currentUserId)
        {
            return Ok(booking);
        }

        return Forbid();
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<BookingDto>> CreateBooking(
        [FromBody] CreateBookingRequest request,
        CancellationToken ct)
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            
            if (request.CustomerId != currentUserId)
            {
                return Forbid();
            }

            var booking = await _bookingService.CreateBookingAsync(request, ct);
            return CreatedAtAction(nameof(GetBookingById), new { id = booking.Id }, booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<BookingDto>> ConfirmBooking(
        Guid id,
        [FromBody] BookingDecisionRequest request,
        CancellationToken ct)
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var booking = await _bookingService.GetBookingByIdAsync(id, ct);
            if (booking is null)
                return NotFound(new { message = $"Booking with ID {id} not found" });

            if (userRole == "Admin")
            {
                var venue = await _venueService.GetVenueByIdAsync(booking.VenueId, ct);
                if (venue?.UserId != currentUserId)
                {
                    return Forbid();
                }
            }

            var confirmedBooking = await _bookingService.ConfirmBookingAsync(id, request, ct);
            return Ok(confirmedBooking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/decline")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<BookingDto>> DeclineBooking(
        Guid id,
        [FromBody] BookingDecisionRequest request,
        CancellationToken ct)
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var booking = await _bookingService.GetBookingByIdAsync(id, ct);
            if (booking is null)
                return NotFound(new { message = $"Booking with ID {id} not found" });

            if (userRole == "Admin")
            {
                var venue = await _venueService.GetVenueByIdAsync(booking.VenueId, ct);
                if (venue?.UserId != currentUserId)
                {
                    return Forbid();
                }
            }

            var declinedBooking = await _bookingService.DeclineBookingAsync(id, request, ct);
            return Ok(declinedBooking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<BookingDto>> CancelBooking(
        Guid id,
        [FromBody] CancelBookingRequest request,
        CancellationToken ct)
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var booking = await _bookingService.GetBookingByIdAsync(id, ct);
            if (booking is null)
                return NotFound(new { message = $"Booking with ID {id} not found" });

            if (userRole == "SuperAdmin")
            {
                var cancelledBooking = await _bookingService.CancelBookingAsync(id, request, ct);
                return Ok(cancelledBooking);
            }

            if (userRole == "Admin")
            {
                var venue = await _venueService.GetVenueByIdAsync(booking.VenueId, ct);
                if (venue?.UserId != currentUserId)
                {
                    return Forbid();
                }
                var cancelledBooking = await _bookingService.CancelBookingAsync(id, request, ct);
                return Ok(cancelledBooking);
            }

            // Customer can only cancel their own bookings
            if (userRole == "Customer" && booking.CustomerId == currentUserId)
            {
                var cancelledBooking = await _bookingService.CancelBookingAsync(id, request, ct);
                return Ok(cancelledBooking);
            }

            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("availability")]
    [AllowAnonymous]
    public async Task<ActionResult<AvailabilityDto>> CheckAvailability(
        [FromQuery] Guid venueId,
        [FromQuery] string date,
        [FromQuery] int partySize,
        CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            return BadRequest(new { message = "Invalid date format. Use YYYY-MM-DD" });
        }

        if (partySize <= 0)
        {
            return BadRequest(new { message = "Party size must be greater than 0" });
        }

        var availability = await _bookingService.CheckAvailabilityAsync(venueId, parsedDate, partySize, ct);
        return Ok(availability);
    }
}