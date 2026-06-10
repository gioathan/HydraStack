using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Ratings;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Services.Ratings;
using Hydra.Api.Services.Venues;
using Hydra.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class VenuesController : ControllerBase
{
    private readonly IVenueService _venueService;
    private readonly IRatingService _ratingService;

    public VenuesController(IVenueService venueService, IRatingService ratingService)
    {
        _venueService = venueService;
        _ratingService = ratingService;
    }

    [HttpGet("locations")]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetLocations(CancellationToken ct)
    {
        return Ok(await _venueService.GetLocationsAsync(ct));
    }

    [HttpGet]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    public async Task<ActionResult<PagedResult<VenueDto>>> GetAllVenues(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] Guid? venueTypeId = null,
        [FromQuery] string? name = null,
        [FromQuery] string? location = null,
        CancellationToken ct = default)
    {
        return Ok(await _venueService.GetAllVenuesAsync(page, pageSize, venueTypeId, name, location, ct));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    public async Task<ActionResult<VenueDto>> GetVenueById(Guid id, CancellationToken ct)
    {
        var venue = await _venueService.GetVenueByIdAsync(id, ct);
        if (venue is null)
            return NotFound(new { message = $"Venue with ID {id} not found" });

        return Ok(venue);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<VenueDto>> UpdateVenue(
        Guid id,
        [FromBody] UpdateVenueRequest request,
        CancellationToken ct)
    {
        if (User.GetRole() == "Admin")
        {
            var existing = await _venueService.GetVenueByIdAsync(id, ct);
            if (existing is null)
                return NotFound(new { message = $"Venue with ID {id} not found" });

            if (existing.UserId != User.GetUserId())
                return Forbid();
        }

        var venue = await _venueService.UpdateVenueAsync(id, request, ct);
        if (venue is null)
            return NotFound(new { message = $"Venue with ID {id} not found" });

        return Ok(venue);
    }

    [HttpPost("{id:guid}/photos")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<VenuePhotoDto>> AddPhoto(
        Guid id,
        [FromBody] AddVenuePhotoRequest request,
        CancellationToken ct)
    {
        if (User.GetRole() == "Admin")
        {
            var venue = await _venueService.GetVenueByIdAsync(id, ct);
            if (venue is null)
                return NotFound(new { message = $"Venue with ID {id} not found" });
            if (venue.UserId != User.GetUserId())
                return Forbid();
        }

        var photo = await _venueService.AddPhotoAsync(id, request, ct);
        if (photo is null)
            return NotFound(new { message = $"Venue with ID {id} not found" });

        return CreatedAtAction(nameof(GetVenueById), new { id }, photo);
    }

    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeletePhoto(Guid id, Guid photoId, CancellationToken ct)
    {
        if (User.GetRole() == "Admin")
        {
            var venue = await _venueService.GetVenueByIdAsync(id, ct);
            if (venue is null)
                return NotFound(new { message = $"Venue with ID {id} not found" });
            if (venue.UserId != User.GetUserId())
                return Forbid();
        }

        var deleted = await _venueService.DeletePhotoAsync(id, photoId, ct);
        if (!deleted)
            return NotFound(new { message = $"Photo with ID {photoId} not found on venue {id}" });

        return NoContent();
    }

    [HttpGet("{id:guid}/rules")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<BookingRulesDto>> GetBookingRules(Guid id, CancellationToken ct)
    {
        if (User.GetRole() == "Admin")
        {
            var venue = await _venueService.GetVenueByIdAsync(id, ct);
            if (venue is null)
                return NotFound(new { message = $"Venue with ID {id} not found" });
            if (venue.UserId != User.GetUserId())
                return Forbid();
        }

        var rules = await _venueService.GetBookingRulesAsync(id, ct);
        if (rules is null)
            return NotFound(new { message = $"Booking rules for venue {id} not found" });

        return Ok(rules);
    }

    [HttpPatch("{id:guid}/rules")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<BookingRulesDto>> UpdateBookingRules(
        Guid id,
        [FromBody] UpdateBookingRulesRequest request,
        CancellationToken ct)
    {
        if (User.GetRole() == "Admin")
        {
            var venue = await _venueService.GetVenueByIdAsync(id, ct);
            if (venue is null)
                return NotFound(new { message = $"Venue with ID {id} not found" });
            if (venue.UserId != User.GetUserId())
                return Forbid();
        }

        var rules = await _venueService.UpdateBookingRulesAsync(id, request, ct);
        if (rules is null)
            return NotFound(new { message = $"Booking rules for venue {id} not found" });

        return Ok(rules);
    }

    [HttpPut("{id:guid}/pricing")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<IReadOnlyList<VenuePricingItemDto>>> SetPricing(
        Guid id,
        [FromBody] SetVenuePricingRequest request,
        CancellationToken ct)
    {
        if (User.GetRole() == "Admin")
        {
            var venue = await _venueService.GetVenueByIdAsync(id, ct);
            if (venue is null)
                return NotFound(new { message = $"Venue with ID {id} not found" });
            if (venue.UserId != User.GetUserId())
                return Forbid();
        }

        var items = await _venueService.SetVenuePricingAsync(id, request, ct);
        if (items is null)
            return NotFound(new { message = $"Venue with ID {id} not found" });

        return Ok(items);
    }

    [HttpPost("{id:guid}/rate")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> RateVenue(
        Guid id,
        [FromBody] SubmitRatingRequest request,
        CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
            return Forbid();

        var (success, error) = await _ratingService.SubmitRatingAsync(id, customerId.Value, request, ct);
        if (!success)
            return BadRequest(new { message = error });

        return NoContent();
    }

    [HttpPatch("{id:guid}/bookings-enabled")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<VenueDto>> ToggleBookings(Guid id, [FromBody] ToggleBookingsRequest request, CancellationToken ct)
    {
        if (User.GetRole() == "Admin")
        {
            var existing = await _venueService.GetVenueByIdAsync(id, ct);
            if (existing is null) return NotFound(new { message = $"Venue with ID {id} not found" });
            if (existing.UserId != User.GetUserId()) return Forbid();
        }

        var venue = await _venueService.ToggleBookingsAsync(id, request.Enabled, ct);
        if (venue is null) return NotFound(new { message = $"Venue with ID {id} not found" });
        return Ok(venue);
    }

    [HttpPatch("{id:guid}/events-enabled")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<VenueDto>> ToggleEvents(Guid id, [FromBody] ToggleEventsRequest request, CancellationToken ct)
    {
        if (User.GetRole() == "Admin")
        {
            var existing = await _venueService.GetVenueByIdAsync(id, ct);
            if (existing is null) return NotFound(new { message = $"Venue with ID {id} not found" });
            if (existing.UserId != User.GetUserId()) return Forbid();
        }

        var venue = await _venueService.ToggleEventsAsync(id, request.Enabled, ct);
        if (venue is null) return NotFound(new { message = $"Venue with ID {id} not found" });
        return Ok(venue);
    }

    [HttpPut("{id:guid}/photos/order")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<IReadOnlyList<VenuePhotoDto>>> ReorderPhotos(
        Guid id,
        [FromBody] ReorderVenuePhotosRequest request,
        CancellationToken ct)
    {
        if (User.GetRole() == "Admin")
        {
            var venue = await _venueService.GetVenueByIdAsync(id, ct);
            if (venue is null)
                return NotFound(new { message = $"Venue with ID {id} not found" });
            if (venue.UserId != User.GetUserId())
                return Forbid();
        }

        var photos = await _venueService.ReorderPhotosAsync(id, request, ct);
        if (photos is null)
            return NotFound(new { message = $"Venue with ID {id} not found" });

        return Ok(photos);
    }
}
