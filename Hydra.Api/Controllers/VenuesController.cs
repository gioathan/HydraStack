using Hydra.Api.Contracts.Common;
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
    public async Task<ActionResult<PagedResult<VenueDto>>> GetAllVenues(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] Guid? venueTypeId = null,
        [FromQuery] string? name = null,
        CancellationToken ct = default)
    {
        return Ok(await _venueService.GetAllVenuesAsync(page, pageSize, venueTypeId, name, ct));
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
