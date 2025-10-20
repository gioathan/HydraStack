namespace Hydra.Api.Models;

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,   // accepted by venue admin
    Declined = 2,   // explicitly rejected by venue admin
    Cancelled = 3,   // customer (or admin) cancels after being confirmed
    Seated = 4,
    NoShow = 5
}

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = default!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public DateTime StartUtc { get; set; }      // request slot (UTC)
    public DateTime EndUtc { get; set; }        // request slot end (UTC)
    public int PartySize { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    // NEW: timeline & audit (simple, no users table yet)
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAtUtc { get; set; }         // when confirmed/declined
    public string? DecidedBy { get; set; }              // admin display/email (optional)

    // Optional notes (nice for MVP UI)
    public string? CustomerNote { get; set; }
    public string? AdminNote { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}