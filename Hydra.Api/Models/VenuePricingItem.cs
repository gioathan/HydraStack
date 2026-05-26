namespace Hydra.Api.Models;

public class VenuePricingItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = default!;
    public string? Category { get; set; }
    public string Title { get; set; } = default!;
    public string? Subtitle { get; set; }
    public decimal Price { get; set; }
    public int DisplayOrder { get; set; }
}
