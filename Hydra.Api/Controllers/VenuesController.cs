using Hydra.Api.Contracts.Venues;
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

    public VenuesController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    [HttpGet]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    public async Task<ActionResult<List<VenueDto>>> GetAllVenues(CancellationToken ct)
    {
        return Ok(await _venueService.GetAllVenuesAsync(ct));
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
}