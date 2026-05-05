using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Services.Venues;
using Hydra.Api.Services.GooglePlaces;
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
    private readonly IGooglePlacesService _googlePlacesService;

    public VenuesController(IVenueService venueService, IGooglePlacesService googlePlacesService)
    {
        _venueService = venueService;
        _googlePlacesService = googlePlacesService;
    }

    [HttpGet]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    public async Task<ActionResult<PagedResult<VenueDto>>> GetAllVenues(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        return Ok(await _venueService.GetAllVenuesAsync(page, pageSize, ct));
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

    [HttpGet("{id:guid}/photo")]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    public async Task<IActionResult> GetVenuePhoto(
        Guid id,
        [FromQuery] int maxWidth = 800,
        CancellationToken ct = default)
    {
        var venue = await _venueService.GetVenueByIdAsync(id, ct);
        if (venue is null)
            return NotFound();
        if (string.IsNullOrEmpty(venue.GooglePlaceId))
            return NoContent();

        var url = await _googlePlacesService.GetPhotoUrlAsync(venue.GooglePlaceId, maxWidth, ct);
        if (url is null)
            return NoContent();

        return Redirect(url);
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
}
