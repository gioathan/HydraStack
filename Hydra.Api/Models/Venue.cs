namespace Hydra.Api.Models;

public class Venue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid VenueTypeId { get; set; }
    public VenueType VenueType { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Address { get; set; } = "";
    public int Capacity { get; set; } = 40;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? GooglePlaceId { get; set; }

    public BookingRules? Rules { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}