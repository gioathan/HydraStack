namespace Hydra.Api.Models;

public class VenueEventPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VenueEventId { get; set; }
    public VenueEvent Event { get; set; } = default!;
    public string Url { get; set; } = default!;
    public int DisplayOrder { get; set; }
}
