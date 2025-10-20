namespace Hydra.Api.Models;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Locale { get; set; } = "en";
    public bool MarketingOptIn { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
