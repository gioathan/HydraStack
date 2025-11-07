namespace Hydra.Api.Models;

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1, 
    Declined = 2, 
    Cancelled = 3,   
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

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}