using Hydra.Api.Contracts.Bookings;
using Hydra.Api.Services.Bookings;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BookingDto>>> GetBookings(
        [FromQuery] Guid? venueId,
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var bookings = await _bookingService.GetAllBookingsAsync(venueId, customerId, status, ct);
        return Ok(bookings);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> GetBookingById(Guid id, CancellationToken ct)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id, ct);

        if (booking is null)
            return NotFound(new { message = $"Booking with ID {id} not found" });

        return Ok(booking);
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> CreateBooking(
        [FromBody] CreateBookingRequest request,
        CancellationToken ct)
    {
        try
        {
            var booking = await _bookingService.CreateBookingAsync(request, ct);
            return CreatedAtAction(nameof(GetBookingById), new { id = booking.Id }, booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<BookingDto>> ConfirmBooking(
        Guid id,
        [FromBody] BookingDecisionRequest request,
        CancellationToken ct)
    {
        try
        {
            var booking = await _bookingService.ConfirmBookingAsync(id, request, ct);

            if (booking is null)
                return NotFound(new { message = $"Booking with ID {id} not found" });

            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/decline")]
    public async Task<ActionResult<BookingDto>> DeclineBooking(
        Guid id,
        [FromBody] BookingDecisionRequest request,
        CancellationToken ct)
    {
        try
        {
            var booking = await _bookingService.DeclineBookingAsync(id, request, ct);

            if (booking is null)
                return NotFound(new { message = $"Booking with ID {id} not found" });

            return Ok(booking);
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
            var booking = await _bookingService.CancelBookingAsync(id, request, ct);

            if (booking is null)
                return NotFound(new { message = $"Booking with ID {id} not found" });

            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("availability")]
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