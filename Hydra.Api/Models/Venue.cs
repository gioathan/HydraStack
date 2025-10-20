namespace Hydra.Api.Models;

public class Venue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Address { get; set; } = "";
    public int Capacity { get; set; } = 40;

    public BookingRules? Rules { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
