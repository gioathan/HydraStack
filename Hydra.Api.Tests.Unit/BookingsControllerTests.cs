using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Hydra.Api.Contracts.Bookings;
using Hydra.Api.Controllers;
using Hydra.Api.Data;
using Hydra.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Tests.Unit;

public class BookingsControllerTests
{
    private static AppDbContext NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(opts);

        // seed one venue + one customer
        var v = new Venue { Name = "Test", Address = "Addr", Capacity = 10 };
        var c = new Customer { Email = "guest@example.com" };
        db.Venues.Add(v);
        db.Customers.Add(c);
        db.SaveChanges();

        return db;
    }

    [Fact]
    public async Task Create_Pending_Request_Succeeds()
    {
        using var db = NewDb();
        var ctl = new BookingsController(db);

        var venueId = await db.Venues.Select(v => v.Id).FirstAsync();
        var customerId = await db.Customers.Select(c => c.Id).FirstAsync();

        var now = DateTime.UtcNow;
        var req = new CreateBookingRequest(
            VenueId: venueId,
            CustomerId: customerId,
            StartUtc: now.AddHours(1),
            EndUtc: now.AddHours(2),
            PartySize: 2,
            CustomerNote: "Window seat"
        );

        var result = await ctl.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;

        created.Should().NotBeNull();
        var dto = created!.Value as BookingDto;
        dto.Should().NotBeNull();
        dto!.Status.Should().Be(nameof(BookingStatus.Pending));
        dto.CustomerNote.Should().Be("Window seat");
    }

    [Fact]
    public async Task Confirm_From_Pending_Succeeds_Second_Confirm_Conflicts()
    {
        using var db = NewDb();
        var ctl = new BookingsController(db);

        var vId = await db.Venues.Select(v => v.Id).FirstAsync();
        var cId = await db.Customers.Select(c => c.Id).FirstAsync();
        var now = DateTime.UtcNow;

        // create pending
        var create = await ctl.Create(new CreateBookingRequest(
            vId, cId, now.AddHours(1), now.AddHours(2), 2, null), CancellationToken.None);
        var created = (create.Result as CreatedAtActionResult)!.Value as BookingDto;
        var id = created!.Id;

        // confirm once
        var ok = await ctl.Confirm(id, new BookingDecisionRequest("admin@venue", "ok"),
            CancellationToken.None);
        ok.Should().BeOfType<NoContentResult>();

        // confirm again -> conflict (only pending can be confirmed)
        var again = await ctl.Confirm(id, new BookingDecisionRequest("admin@venue", "again"),
            CancellationToken.None);
        var conflict = again as ObjectResult;
        conflict!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Invalid_Time_Range_Returns_BadRequest()
    {
        using var db = NewDb();
        var ctl = new BookingsController(db);

        var vId = await db.Venues.Select(v => v.Id).FirstAsync();
        var cId = await db.Customers.Select(c => c.Id).FirstAsync();
        var now = DateTime.UtcNow;

        var bad = await ctl.Create(new CreateBookingRequest(
            vId, cId, now.AddHours(2), now.AddHours(1), 2, null), CancellationToken.None);

        var br = bad.Result as BadRequestObjectResult;
        br.Should().NotBeNull();
    }
}
