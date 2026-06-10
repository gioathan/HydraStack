namespace Hydra.Api.Models;

public class VenueEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public string? MainPhotoUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<VenueEventPhoto> AdditionalPhotos { get; set; } = new List<VenueEventPhoto>();

    public bool IsPast =>
        (EndsAtUtc.HasValue && EndsAtUtc.Value < DateTime.UtcNow) ||
        ClosedAtUtc.HasValue;
}
