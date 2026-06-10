using Hydra.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Data;

public class DatabaseSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(AppDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting database seed...");
        await SeedVenueTypesAsync(ct);
        await SeedSuperAdminAsync(ct);
        await SeedVenuesAsync(ct);
        await SeedCustomersAsync(ct);
        await SeedBookingsAsync(ct);
        _logger.LogInformation("Database seed complete.");
    }

    // ── STEP 2 ──────────────────────────────────────────────────────────────

    private async Task SeedVenueTypesAsync(CancellationToken ct)
    {
        var types = new[]
        {
            ("Restaurant", 1),
            ("Cafe",       2),
            ("Bar",        3),
            ("Beach Bar",  4),
            ("Boat Trip",  5),
            ("Club",       6),
            ("Wine Bar",   7)
        };

        foreach (var (name, order) in types)
        {
            try
            {
                if (await _context.VenueTypes.AnyAsync(vt => vt.Name == name, ct))
                    continue;

                _context.VenueTypes.Add(new VenueType
                {
                    Name = name,
                    DisplayOrder = order,
                    Description = null
                });

                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Seeded venue type: {Name}", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed venue type: {Name}", name);
            }
        }
    }

    // ── STEP 3 ──────────────────────────────────────────────────────────────

    private async Task SeedSuperAdminAsync(CancellationToken ct)
    {
        const string email = "superadmin@hydra.app";
        const string password = "Admin@12345!";

        try
        {
            if (await _context.Users.AnyAsync(u => u.Email == email, ct))
                return;

            _context.Users.Add(new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.SuperAdmin,
                IsEmailVerified = true
            });

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded SuperAdmin — email: {Email} password: {Password}", email, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed SuperAdmin user");
        }
    }

    // ── STEP 4 ──────────────────────────────────────────────────────────────

    // For dev venues GooglePlaceIds use the "picsum:<seed>" prefix so the backend
    // returns https://picsum.photos/seed/<seed>/800/600 without hitting Google.
    // In production, real GooglePlaceId values never start with "picsum:".

    private async Task SeedVenuesAsync(CancellationToken ct)
    {
        var venueData = new[]
        {
            new VenueSeed(
                AdminEmail:    "admin.sunset@hydra.app",
                VenueName:     "Sunset Terrace",
                Address:       "Miaouli 12, Hydra",
                Capacity:      45,
                VenueTypeName: "Restaurant",
                PhotoSeeds:    ["picsum:sunset-terrace", "picsum:sunset-terrace-2", "picsum:sunset-terrace-3"],
                SlotMinutes:   90,  AutoConfirm: true,  OpenHour: 12, CloseHour: 23,
                Location:      "Hydra", Latitude: 37.3490, Longitude: 23.4735),

            new VenueSeed(
                AdminEmail:    "admin.harbor@hydra.app",
                VenueName:     "Harbor View Cafe",
                Address:       "Tombazi 3, Hydra",
                Capacity:      30,
                VenueTypeName: "Cafe",
                PhotoSeeds:    ["picsum:harbor-view-cafe", "picsum:harbor-view-cafe-2"],
                SlotMinutes:   60,  AutoConfirm: true,  OpenHour: 8,  CloseHour: 20,
                Location:      "Hydra", Latitude: 37.3488, Longitude: 23.4725),

            new VenueSeed(
                AdminEmail:    "admin.bluebar@hydra.app",
                VenueName:     "The Blue Bar",
                Address:       "Lignou 7, Hydra",
                Capacity:      50,
                VenueTypeName: "Bar",
                PhotoSeeds:    ["picsum:the-blue-bar", "picsum:the-blue-bar-2", "picsum:the-blue-bar-3"],
                SlotMinutes:   120, AutoConfirm: false, OpenHour: 18, CloseHour: 2,
                Location:      "Hydra", Latitude: 37.3483, Longitude: 23.4722),

            new VenueSeed(
                AdminEmail:    "admin.crystalbeach@hydra.app",
                VenueName:     "Crystal Beach Bar",
                Address:       "Mandraki Beach, Hydra",
                Capacity:      60,
                VenueTypeName: "Beach Bar",
                PhotoSeeds:    ["picsum:crystal-beach-bar", "picsum:crystal-beach-bar-2"],
                SlotMinutes:   90,  AutoConfirm: true,  OpenHour: 10, CloseHour: 20,
                Location:      "Hydra", Latitude: 37.3530, Longitude: 23.4820),

            new VenueSeed(
                AdminEmail:    "admin.poseidon@hydra.app",
                VenueName:     "Poseidon Boat Trips",
                Address:       "Main Port, Hydra",
                Capacity:      12,
                VenueTypeName: "Boat Trip",
                PhotoSeeds:    ["picsum:poseidon-boat-trips", "picsum:poseidon-boat-trips-2"],
                SlotMinutes:   180, AutoConfirm: false, OpenHour: 9,  CloseHour: 18,
                Location:      "Hydra", Latitude: 37.3493, Longitude: 23.4728),

            new VenueSeed(
                AdminEmail:    "admin.acropolis@hydra.app",
                VenueName:     "Acropolis Restaurant",
                Address:       "Votsi 15, Hydra",
                Capacity:      35,
                VenueTypeName: "Restaurant",
                PhotoSeeds:    ["picsum:acropolis-restaurant", "picsum:acropolis-restaurant-2", "picsum:acropolis-restaurant-3"],
                SlotMinutes:   90,  AutoConfirm: true,  OpenHour: 13, CloseHour: 23,
                Location:      "Hydra", Latitude: 37.3478, Longitude: 23.4745),

            new VenueSeed(
                AdminEmail:    "admin.aegean@hydra.app",
                VenueName:     "Aegean Breeze Bar",
                Address:       "Spilia Beach, Hydra",
                Capacity:      40,
                VenueTypeName: "Bar",
                PhotoSeeds:    ["picsum:aegean-breeze-bar", "picsum:aegean-breeze-bar-2"],
                SlotMinutes:   60,  AutoConfirm: true,  OpenHour: 17, CloseHour: 1,
                Location:      "Hydra", Latitude: 37.3495, Longitude: 23.4705),

            new VenueSeed(
                AdminEmail:    "admin.hydracafe@hydra.app",
                VenueName:     "Hydra Coffee House",
                Address:       "Tombazi 22, Hydra",
                Capacity:      20,
                VenueTypeName: "Cafe",
                PhotoSeeds:    ["picsum:hydra-coffee-house", "picsum:hydra-coffee-house-2"],
                SlotMinutes:   45,  AutoConfirm: true,  OpenHour: 7,  CloseHour: 19,
                Location:      "Hydra", Latitude: 37.3486, Longitude: 23.4729)
        };

        foreach (var seed in venueData)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == seed.AdminEmail, ct))
                    continue;

                var venueType = await _context.VenueTypes
                    .FirstOrDefaultAsync(vt => vt.Name == seed.VenueTypeName, ct);

                if (venueType is null)
                {
                    _logger.LogError("VenueType '{Type}' not found — skipping venue {Name}",
                        seed.VenueTypeName, seed.VenueName);
                    continue;
                }

                var adminUser = new User
                {
                    Email = seed.AdminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@12345!"),
                    Role = UserRole.Admin,
                    IsEmailVerified = true
                };
                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync(ct);

                var venue = new Venue
                {
                    UserId = adminUser.Id,
                    VenueTypeId = venueType.Id,
                    Name = seed.VenueName,
                    Address = seed.Address,
                    Capacity = seed.Capacity,
                    Location = seed.Location,
                    Latitude = seed.Latitude,
                    Longitude = seed.Longitude,
                    BookingsEnabled = true,
                    EventsEnabled = true
                };
                _context.Venues.Add(venue);
                await _context.SaveChangesAsync(ct);

                for (var i = 0; i < seed.PhotoSeeds.Length; i++)
                {
                    _context.VenuePhotos.Add(new VenuePhoto
                    {
                        VenueId = venue.Id,
                        GooglePlaceId = seed.PhotoSeeds[i],
                        DisplayOrder = i
                    });
                }
                await _context.SaveChangesAsync(ct);

                _context.BookingRules.Add(new BookingRules
                {
                    VenueId = venue.Id,
                    SlotMinutes = seed.SlotMinutes,
                    AutoConfirm = seed.AutoConfirm,
                    OpenHour = seed.OpenHour,
                    CloseHour = seed.CloseHour
                });
                await _context.SaveChangesAsync(ct);

                _logger.LogInformation("Seeded venue: {Name} (admin: {Email})", seed.VenueName, seed.AdminEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed venue: {Name}", seed.VenueName);
            }
        }
    }

    private record VenueSeed(
        string AdminEmail,
        string VenueName,
        string Address,
        int Capacity,
        string VenueTypeName,
        string[] PhotoSeeds,
        int SlotMinutes,
        bool AutoConfirm,
        int OpenHour,
        int CloseHour,
        string? Location = null,
        double? Latitude = null,
        double? Longitude = null);

    // ── STEP 5 ──────────────────────────────────────────────────────────────

    private async Task SeedCustomersAsync(CancellationToken ct)
    {
        var customers = new[]
        {
            new CustomerSeed("giorgos@test.com", "Customer@123!", "Giorgos Papadopoulos", "+306912345678", "el"),
            new CustomerSeed("sarah@test.com",   "Customer@123!", "Sarah Johnson",         "+447700900123", "en"),
            new CustomerSeed("marco@test.com",   "Customer@123!", "Marco Rossi",           "+393331234567", "en")
        };

        foreach (var seed in customers)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == seed.Email, ct))
                    continue;

                var user = new User
                {
                    Email = seed.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(seed.Password),
                    Role = UserRole.Customer,
                    IsEmailVerified = true
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync(ct);

                _context.Customers.Add(new Customer
                {
                    UserId = user.Id,
                    Name = seed.Name,
                    Email = seed.Email,
                    Phone = seed.Phone,
                    Locale = seed.Locale,
                    CreatedAtUtc = DateTime.UtcNow
                });
                await _context.SaveChangesAsync(ct);

                _logger.LogInformation("Seeded customer: {Name} ({Email})", seed.Name, seed.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed customer: {Email}", seed.Email);
            }
        }
    }

    private record CustomerSeed(string Email, string Password, string Name, string Phone, string Locale);

    // ── STEP 6 ──────────────────────────────────────────────────────────────

    private async Task SeedBookingsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var bookingSpecs = new[]
        {
            // Giorgos
            new BookingSpec("giorgos@test.com", "Sunset Terrace",
                StartUtc: now.Date.AddDays(3).AddHours(19),
                Duration: 90, PartySize: 2, Status: BookingStatus.Confirmed),

            new BookingSpec("giorgos@test.com", "The Blue Bar",
                StartUtc: now.Date.AddDays(7).AddHours(20),
                Duration: 120, PartySize: 4, Status: BookingStatus.Pending),

            new BookingSpec("giorgos@test.com", "Harbor View Cafe",
                StartUtc: now.Date.AddDays(-30).AddHours(10),
                Duration: 60, PartySize: 1, Status: BookingStatus.Confirmed),

            // Sarah
            new BookingSpec("sarah@test.com", "Poseidon Boat Trips",
                StartUtc: now.Date.AddDays(2).AddHours(10),
                Duration: 180, PartySize: 6, Status: BookingStatus.Pending),

            new BookingSpec("sarah@test.com", "Crystal Beach Bar",
                StartUtc: now.Date.AddDays(-14).AddHours(13),
                Duration: 90, PartySize: 3, Status: BookingStatus.Cancelled),

            // Marco
            new BookingSpec("marco@test.com", "Acropolis Restaurant",
                StartUtc: now.Date.AddDays(5).AddHours(20).AddMinutes(30),
                Duration: 90, PartySize: 2, Status: BookingStatus.Confirmed),

            new BookingSpec("marco@test.com", "Aegean Breeze Bar",
                StartUtc: now.Date.AddDays(-21).AddHours(18),
                Duration: 60, PartySize: 5, Status: BookingStatus.Declined)
        };

        // Group by customer email and skip entire customer if they have any bookings
        var customerEmails = bookingSpecs.Select(b => b.CustomerEmail).Distinct();

        foreach (var email in customerEmails)
        {
            try
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == email, ct);

                if (user is null)
                {
                    _logger.LogWarning("Customer user '{Email}' not found — skipping their bookings", email);
                    continue;
                }

                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == user.Id, ct);

                if (customer is null)
                {
                    _logger.LogWarning("Customer profile for '{Email}' not found — skipping their bookings", email);
                    continue;
                }

                // Idempotency: skip if this customer already has any bookings
                if (await _context.Bookings.AnyAsync(b => b.CustomerId == customer.Id, ct))
                {
                    _logger.LogInformation("Customer '{Email}' already has bookings — skipping", email);
                    continue;
                }

                foreach (var spec in bookingSpecs.Where(s => s.CustomerEmail == email))
                {
                    try
                    {
                        var venue = await _context.Venues
                            .AsNoTracking()
                            .FirstOrDefaultAsync(v => v.Name == spec.VenueName, ct);

                        if (venue is null)
                        {
                            _logger.LogWarning("Venue '{Venue}' not found — skipping booking", spec.VenueName);
                            continue;
                        }

                        var isPast = spec.StartUtc < now;
                        var createdAt = isPast
                            ? spec.StartUtc.AddDays(-3)
                            : now;

                        _context.Bookings.Add(new Booking
                        {
                            VenueId = venue.Id,
                            CustomerId = customer.Id,
                            StartUtc = spec.StartUtc,
                            EndUtc = spec.StartUtc.AddMinutes(spec.Duration),
                            PartySize = spec.PartySize,
                            Status = spec.Status,
                            CreatedAtUtc = createdAt,
                            UpdatedAtUtc = createdAt
                        });

                        await _context.SaveChangesAsync(ct);
                        _logger.LogInformation(
                            "Seeded booking: {Customer} @ {Venue} on {Start:yyyy-MM-dd HH:mm} UTC",
                            email, spec.VenueName, spec.StartUtc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to seed booking for {Customer} @ {Venue}", email, spec.VenueName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process bookings for customer '{Email}'", email);
            }
        }
    }

    private record BookingSpec(
        string CustomerEmail,
        string VenueName,
        DateTime StartUtc,
        int Duration,
        int PartySize,
        BookingStatus Status);
}
