namespace Hydra.Api.Models;

public class VenueType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;

    public ICollection<Venue> Venues { get; set; } = new List<Venue>();
}