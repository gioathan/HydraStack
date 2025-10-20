using Microsoft.EntityFrameworkCore;
using Hydra.Api.Models;

namespace Hydra.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Booking> Bookings => Set<Booking>();
    // Remove for now to keep schema simple:
    // public DbSet<BookingRules> BookingRules => Set<BookingRules>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Venue
        b.Entity<Venue>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Address).IsRequired();
            e.HasIndex(x => x.Name);
        });

        // Booking
        b.Entity<Booking>(e =>
        {
            e.HasKey(x => x.Id);

            // helpful indexes for listing/search
            e.HasIndex(x => new { x.VenueId, x.StartUtc });
            e.HasIndex(x => new { x.CustomerId, x.StartUtc });

            // enum → int (explicit for clarity)
            e.Property(x => x.Status).HasConversion<int>();

            // minimal integrity: end after start
            e.HasCheckConstraint("CK_Bookings_StartBeforeEnd", "\"EndUtc\" > \"StartUtc\"");

            // FK relations
            e.HasOne(x => x.Venue)
             .WithMany(v => v.Bookings)
             .HasForeignKey(x => x.VenueId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Customer)
             .WithMany(c => c.Bookings)
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Customer
        b.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email);
            e.HasIndex(x => x.Phone);
        });

        base.OnModelCreating(b);
    }
}
