using Hydra.Api.Contracts.Bookings;
using Hydra.Api.Data;
using Hydra.Api.Mapping;
using Hydra.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BookingsController(AppDbContext db) => _db = db;

    /// <summary>
    /// Create a new booking request (PENDING status)
    /// POST /api/bookings
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(
        [FromBody] CreateBookingRequest request,
        CancellationToken ct)
    {
        // Validation
        if (request.PartySize <= 0)
        {
            return BadRequest(new { error = "PartySize must be greater than 0." });
        }

        if (request.EndUtc <= request.StartUtc)
        {
            return BadRequest(new { error = "End time must be after start time." });
        }

        // Verify venue and customer exist
        var venueExists = await _db.Venues.AnyAsync(v => v.Id == request.VenueId, ct);
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);

        if (!venueExists || !customerExists)
        {
            return BadRequest(new { error = "Invalid VenueId or CustomerId." });
        }

        // DTO → Model using mapping extension
        var booking = request.ToModel();

        // Ensure UTC (defensive, since ToModel should handle this)
        booking.StartUtc = DateTime.SpecifyKind(booking.StartUtc, DateTimeKind.Utc);
        booking.EndUtc = DateTime.SpecifyKind(booking.EndUtc, DateTimeKind.Utc);

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);

        // Model → DTO using mapping extension
        return CreatedAtAction(
            nameof(Get),
            new { id = booking.Id },
            booking.ToDto());
    }

    /// <summary>
    /// Get a booking by ID
    /// GET /api/bookings/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> Get(Guid id, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (booking is null)
            return NotFound();

        // Model → DTO using mapping extension
        return Ok(booking.ToDto());
    }

    /// <summary>
    /// Search bookings with filters
    /// GET /api/bookings?venueId=&customerId=&status=&fromUtc=&toUtc=
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingDto>>> List(
        [FromQuery] Guid? venueId,
        [FromQuery] Guid? customerId,
        [FromQuery] BookingStatus? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct)
    {
        var query = _db.Bookings.AsNoTracking();

        // Apply filters
        if (venueId.HasValue)
            query = query.Where(b => b.VenueId == venueId.Value);

        if (customerId.HasValue)
            query = query.Where(b => b.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        if (fromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc);
            query = query.Where(b => b.StartUtc >= from);
        }

        if (toUtc.HasValue)
        {
            var to = DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc);
            query = query.Where(b => b.StartUtc < to);
        }

        // Execute query
        var bookings = await query
            .OrderByDescending(b => b.StartUtc)
            .ToListAsync(ct);

        // Model → DTO using mapping extension
        var dtos = bookings.Select(b => b.ToDto()).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Confirm a pending booking
    /// POST /api/bookings/{id}/confirm
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(
        Guid id,
        [FromBody] BookingDecisionRequest decision,
        CancellationToken ct)
    {
        var booking = await _db.Bookings.FindAsync([id], ct);

        if (booking is null)
            return NotFound();

        // Use mapping extension method with business logic
        try
        {
            booking.Confirm(
                adminIdentifier: decision.Admin ?? "Unknown Admin",
                note: decision.Note);

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Decline a pending booking
    /// POST /api/bookings/{id}/decline
    /// </summary>
    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(
        Guid id,
        [FromBody] BookingDecisionRequest decision,
        CancellationToken ct)
    {
        var booking = await _db.Bookings.FindAsync([id], ct);

        if (booking is null)
            return NotFound();

        // Use mapping extension method with business logic
        try
        {
            booking.Decline(
                adminIdentifier: decision.Admin ?? "Unknown Admin",
                note: decision.Note);

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel a confirmed booking
    /// POST /api/bookings/{id}/cancel
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelBookingRequest request,
        CancellationToken ct)
    {
        var booking = await _db.Bookings.FindAsync([id], ct);

        if (booking is null)
            return NotFound();

        // Use mapping extension method with business logic
        try
        {
            booking.Cancel(cancelledBy: request?.Reason);

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Mark a confirmed booking as seated
    /// POST /api/bookings/{id}/seated
    /// </summary>
    [HttpPost("{id:guid}/seated")]
    public async Task<IActionResult> MarkSeated(Guid id, CancellationToken ct)
    {
        var booking = await _db.Bookings.FindAsync([id], ct);

        if (booking is null)
            return NotFound();

        try
        {
            booking.MarkAsSeated();

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Mark a confirmed booking as no-show
    /// POST /api/bookings/{id}/noshow
    /// </summary>
    [HttpPost("{id:guid}/noshow")]
    public async Task<IActionResult> MarkNoShow(Guid id, CancellationToken ct)
    {
        var booking = await _db.Bookings.FindAsync([id], ct);

        if (booking is null)
            return NotFound();

        try
        {
            booking.MarkAsNoShow();

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}