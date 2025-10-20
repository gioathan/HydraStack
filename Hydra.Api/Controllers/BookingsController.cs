using Hydra.Api.Contracts.Bookings;
using Hydra.Api.Data;
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

    // POST /api/bookings  (create a PENDING request)
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingRequest req, CancellationToken ct)
    {
        if (req.PartySize <= 0) return BadRequest("PartySize must be > 0.");
        if (req.EndUtc <= req.StartUtc) return BadRequest("End must be after Start.");

        var venueExists = await _db.Venues.AnyAsync(v => v.Id == req.VenueId, ct);
        var custExists = await _db.Customers.AnyAsync(c => c.Id == req.CustomerId, ct);
        if (!venueExists || !custExists) return BadRequest("Invalid VenueId or CustomerId.");

        var b = new Booking
        {
            VenueId = req.VenueId,
            CustomerId = req.CustomerId,
            StartUtc = DateTime.SpecifyKind(req.StartUtc, DateTimeKind.Utc),
            EndUtc = DateTime.SpecifyKind(req.EndUtc, DateTimeKind.Utc),
            PartySize = req.PartySize,
            Status = BookingStatus.Pending,
            CustomerNote = req.CustomerNote,
            RequestedAtUtc = DateTime.UtcNow
        };

        _db.Bookings.Add(b);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = b.Id }, ToDto(b));
    }

    // GET /api/bookings/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> Get(Guid id, CancellationToken ct)
    {
        var b = await _db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return b is null ? NotFound() : Ok(ToDto(b));
    }

    // GET /api/bookings?venueId=&customerId=&status=&fromUtc=&toUtc=
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingDto>>> List(
        Guid? venueId, Guid? customerId, BookingStatus? status,
        DateTime? fromUtc, DateTime? toUtc, CancellationToken ct)
    {
        var q = _db.Bookings.AsNoTracking();

        if (venueId is not null) q = q.Where(b => b.VenueId == venueId);
        if (customerId is not null) q = q.Where(b => b.CustomerId == customerId);
        if (status is not null) q = q.Where(b => b.Status == status);
        if (fromUtc is not null) q = q.Where(b => b.StartUtc >= DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc));
        if (toUtc is not null) q = q.Where(b => b.StartUtc < DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc));

        var list = await q.OrderByDescending(b => b.StartUtc)
                          .Select(b => ToDto(b))
                          .ToListAsync(ct);
        return Ok(list);
    }

    // POST /api/bookings/{id}/confirm
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] BookingDecisionRequest req, CancellationToken ct)
    {
        var b = await _db.Bookings.FindAsync([id], ct);
        if (b is null) return NotFound();
        if (b.Status != BookingStatus.Pending) return Conflict("Only pending bookings can be confirmed.");

        b.Status = BookingStatus.Confirmed;
        b.DecidedAtUtc = DateTime.UtcNow;
        b.DecidedBy = req.Admin;
        b.AdminNote = req.Note ?? b.AdminNote;
        b.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST /api/bookings/{id}/decline
    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id, [FromBody] BookingDecisionRequest req, CancellationToken ct)
    {
        var b = await _db.Bookings.FindAsync([id], ct);
        if (b is null) return NotFound();
        if (b.Status != BookingStatus.Pending) return Conflict("Only pending bookings can be declined.");

        b.Status = BookingStatus.Declined;
        b.DecidedAtUtc = DateTime.UtcNow;
        b.DecidedBy = req.Admin;
        b.AdminNote = req.Note ?? b.AdminNote;
        b.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST /api/bookings/{id}/cancel
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string? reason, CancellationToken ct)
    {
        var b = await _db.Bookings.FindAsync([id], ct);
        if (b is null) return NotFound();
        if (b.Status != BookingStatus.Confirmed) return Conflict("Only confirmed bookings can be cancelled.");

        b.Status = BookingStatus.Cancelled;
        b.AdminNote = reason ?? b.AdminNote;
        b.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static BookingDto ToDto(Booking b) =>
        new(b.Id, b.VenueId, b.CustomerId, b.StartUtc, b.EndUtc, b.PartySize,
            b.Status.ToString(), b.RequestedAtUtc, b.DecidedAtUtc, b.CustomerNote, b.AdminNote);
}
