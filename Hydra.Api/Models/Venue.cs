namespace Hydra.Api.Models;

public class Venue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public Guid VenueTypeId { get; set; }
    public VenueType VenueType { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Address { get; set; } = "";
    public string? Description { get; set; }
    public int Capacity { get; set; } = 40;
    public string? MapsUrl { get; set; }
    public string? Location { get; set; }
    public bool BookingsEnabled { get; set; } = false;
    public bool EventsEnabled { get; set; } = false;

    public BookingRules? Rules { get; set; }
    public ICollection<VenuePhoto> Photos { get; set; } = new List<VenuePhoto>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<VenuePricingItem> PricingItems { get; set; } = new List<VenuePricingItem>();
    public ICollection<VenueEvent> Events { get; set; } = new List<VenueEvent>();
}
