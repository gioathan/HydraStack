namespace Hydra.Api.Models;

public class VenueRating
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = default!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = default!;

    public decimal Value { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
