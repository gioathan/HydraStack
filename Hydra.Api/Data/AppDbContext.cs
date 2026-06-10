using Microsoft.EntityFrameworkCore;
using Hydra.Api.Models;

namespace Hydra.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public virtual DbSet<User> Users => Set<User>();
        public virtual DbSet<VenueType> VenueTypes => Set<VenueType>();
        public virtual DbSet<Venue> Venues => Set<Venue>();
        public virtual DbSet<BookingRules> BookingRules => Set<BookingRules>();
        public virtual DbSet<Customer> Customers => Set<Customer>();
        public virtual DbSet<Booking> Bookings => Set<Booking>();
        public virtual DbSet<VenuePhoto> VenuePhotos => Set<VenuePhoto>();
        public virtual DbSet<VenueRating> VenueRatings => Set<VenueRating>();
        public virtual DbSet<VenuePricingItem> VenuePricingItems => Set<VenuePricingItem>();
        public virtual DbSet<VenueEvent> VenueEvents => Set<VenueEvent>();
        public virtual DbSet<VenueEventPhoto> VenueEventPhotos => Set<VenueEventPhoto>();

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

                b.Property(u => u.IsEmailVerified)
                    .IsRequired()
                    .HasDefaultValue(false);
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
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(v => v.Rules)
                    .WithOne(r => r.Venue)
                    .HasForeignKey<BookingRules>(r => r.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(v => v.Photos)
                    .WithOne(p => p.Venue)
                    .HasForeignKey(p => p.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(v => v.Bookings)
                    .WithOne(bk => bk.Venue)
                    .HasForeignKey(bk => bk.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(v => v.PricingItems)
                    .WithOne(pi => pi.Venue)
                    .HasForeignKey(pi => pi.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(v => v.Events)
                    .WithOne(e => e.Venue)
                    .HasForeignKey(e => e.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.Property(v => v.BookingsEnabled)
                    .HasDefaultValue(false);

                b.Property(v => v.EventsEnabled)
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<VenueEvent>(b =>
            {
                b.HasKey(e => e.Id);

                b.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(256);

                b.Property(e => e.Description)
                    .HasColumnType("text");

                b.Property(e => e.MainPhotoUrl)
                    .HasColumnType("text");

                b.Property(e => e.StartsAtUtc)
                    .IsRequired();

                b.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                b.HasMany(e => e.AdditionalPhotos)
                    .WithOne(p => p.Event)
                    .HasForeignKey(p => p.VenueEventId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(e => new { e.VenueId, e.StartsAtUtc });
            });

            modelBuilder.Entity<VenueEventPhoto>(b =>
            {
                b.HasKey(p => p.Id);

                b.Property(p => p.Url)
                    .IsRequired()
                    .HasColumnType("text");

                b.HasIndex(p => new { p.VenueEventId, p.DisplayOrder });
            });

            modelBuilder.Entity<VenuePricingItem>(b =>
            {
                b.HasKey(pi => pi.Id);

                b.Property(pi => pi.Category)
                    .HasMaxLength(100);

                b.Property(pi => pi.Title)
                    .IsRequired()
                    .HasMaxLength(150);

                b.Property(pi => pi.Subtitle)
                    .HasMaxLength(200);

                b.Property(pi => pi.Price)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");

                b.HasIndex(pi => new { pi.VenueId, pi.DisplayOrder });
            });

            modelBuilder.Entity<VenuePhoto>(b =>
            {
                b.HasKey(p => p.Id);

                b.Property(p => p.GooglePlaceId)
                    .IsRequired()
                    .HasMaxLength(512);

                b.HasIndex(p => new { p.VenueId, p.DisplayOrder });
            });

            modelBuilder.Entity<BookingRules>(b =>
            {
                b.HasKey(r => r.Id);

                b.Property(r => r.SlotMinutes)
                    .IsRequired();

                b.Property(r => r.OpenHour)
                    .IsRequired()
                    .HasDefaultValue(9);

                b.Property(r => r.CloseHour)
                    .IsRequired()
                    .HasDefaultValue(22);

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
                b.Property(c => c.PushToken).HasMaxLength(256);

                b.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

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

                b.Property(x => x.RatingNotificationSentAt).IsRequired(false);
            });

            modelBuilder.Entity<VenueRating>(b =>
            {
                b.HasKey(r => r.Id);

                b.Property(r => r.Value)
                    .IsRequired()
                    .HasColumnType("decimal(3,1)");

                b.HasIndex(r => new { r.VenueId, r.CustomerId }).IsUnique();

                b.HasOne(r => r.Venue)
                    .WithMany()
                    .HasForeignKey(r => r.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(r => r.Customer)
                    .WithMany()
                    .HasForeignKey(r => r.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(r => r.Booking)
                    .WithMany()
                    .HasForeignKey(r => r.BookingId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
