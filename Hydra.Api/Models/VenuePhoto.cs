namespace Hydra.Api.Models;

public class VenuePhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = default!;
    public string GooglePlaceId { get; set; } = default!;
    public int DisplayOrder { get; set; }
}
