using Hydra.Api.Models;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Contracts.Customers;
using Hydra.Api.Contracts.Bookings;

namespace Hydra.Api.Mapping;

public static class MappingExtensions
{
    public static VenueDto ToDto(this Venue v) =>
        new(v.Id, v.Name, v.Address, v.Capacity);

    public static CustomerDto ToDto(this Customer c) =>
        new(c.Id, c.Email, c.Phone, c.Locale, c.MarketingOptIn, c.CreatedAtUtc, null);

    public static BookingDto ToDto(this Booking b) =>
        new(b.Id, b.VenueId, b.CustomerId, b.StartUtc, b.EndUtc, b.PartySize,
            b.Status.ToString(), b.RequestedAtUtc, b.DecidedAtUtc, b.CustomerNote, b.AdminNote);
}
