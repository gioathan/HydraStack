using Hydra.Api.Contracts.Venues;
using Hydra.Api.Services.Venues;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class VenuesController : ControllerBase
{
    private readonly IVenueService _venueService;

    public VenuesController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    [HttpGet]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    public async Task<ActionResult<List<VenueDto>>> GetAllVenues(CancellationToken ct)
    {
        var venues = await _venueService.GetAllVenuesAsync(ct);
        return Ok(venues);
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

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<VenueDto>> CreateVenue(
        [FromBody] CreateVenueRequest request,
        CancellationToken ct)
    {
        var venue = await _venueService.CreateVenueAsync(request, ct);
        return CreatedAtAction(nameof(GetVenueById), new { id = venue.Id }, venue);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<VenueDto>> UpdateVenue(
        Guid id,
        [FromBody] UpdateVenueRequest request,
        CancellationToken ct)
    {
        var venue = await _venueService.UpdateVenueAsync(id, request, ct);

        if (venue is null)
            return NotFound(new { message = $"Venue with ID {id} not found" });

        return Ok(venue);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteVenue(Guid id, CancellationToken ct)
    {
        var deleted = await _venueService.DeleteVenueAsync(id, ct);

        if (!deleted)
            return NotFound(new { message = $"Venue with ID {id} not found" });

        return NoContent();
    }
}