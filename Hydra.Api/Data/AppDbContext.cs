using Microsoft.EntityFrameworkCore;
using Hydra.Api.Models;

namespace Hydra.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<VenueType> VenueTypes => Set<VenueType>();
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<BookingRules> BookingRules => Set<BookingRules>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Booking> Bookings => Set<Booking>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(u => u.Id);

                b.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                b.HasIndex(u => u.Email).IsUnique();

                b.Property(u => u.PasswordHash)
                    .IsRequired();
            });

            modelBuilder.Entity<VenueType>(b =>
            {
                b.HasKey(vt => vt.Id);

                b.Property(vt => vt.Name)
                    .IsRequired()
                    .HasMaxLength(128);

                b.HasMany(vt => vt.Venues)
                    .WithOne(v => v.VenueType)
                    .HasForeignKey(v => v.VenueTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Venue>(b =>
            {
                b.HasKey(v => v.Id);

                b.Property(v => v.Name)
                    .IsRequired()
                    .HasMaxLength(256);

                b.Property(v => v.Address)
                    .HasMaxLength(512);

                b.HasOne(v => v.User)
                    .WithMany()
                    .HasForeignKey(v => v.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(v => v.Rules)
                    .WithOne(r => r.Venue)
                    .HasForeignKey<BookingRules>(r => r.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(v => v.Bookings)
                    .WithOne(bk => bk.Venue)
                    .HasForeignKey(bk => bk.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BookingRules>(b =>
            {
                b.HasKey(r => r.Id);

                b.Property(r => r.SlotMinutes)
                    .IsRequired();

                b.HasIndex(r => r.VenueId).IsUnique();
            });

            modelBuilder.Entity<Customer>(b =>
            {
                b.HasKey(c => c.Id);

                b.Property(c => c.Locale)
                    .IsRequired()
                    .HasMaxLength(10);

                b.Property(c => c.Email).HasMaxLength(256);
                b.Property(c => c.Name).HasMaxLength(256);
                b.Property(c => c.Phone).HasMaxLength(64);

                b.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasMany(c => c.Bookings)
                    .WithOne(bk => bk.Customer)
                    .HasForeignKey(bk => bk.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Booking>(b =>
            {
                b.HasKey(x => x.Id);

                b.Property(x => x.PartySize).IsRequired();

                b.Property(x => x.StartUtc).IsRequired();
                b.Property(x => x.EndUtc).IsRequired();

                b.HasOne(x => x.Venue)
                    .WithMany(v => v.Bookings)
                    .HasForeignKey(x => x.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Customer)
                    .WithMany(c => c.Bookings)
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => new { x.VenueId, x.StartUtc, x.EndUtc });
            });
        }
    }
}
