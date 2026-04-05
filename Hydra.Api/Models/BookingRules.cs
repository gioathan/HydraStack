namespace Hydra.Api.Models;

public class BookingRules
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = default!;

    public int SlotMinutes { get; set; } = 90;
    public bool AutoConfirm { get; set; } = true;
}
