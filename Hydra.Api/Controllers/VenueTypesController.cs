using Asp.Versioning;
using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.VenueTypes;
using Hydra.Api.Services.VenueTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class VenueTypesController : ControllerBase
{
    private readonly IVenueTypeService _venueTypeService;

    public VenueTypesController(IVenueTypeService venueTypeService)
    {
        _venueTypeService = venueTypeService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<VenueTypeDto>>> GetAllVenueTypes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        return Ok(await _venueTypeService.GetAllVenueTypesAsync(page, pageSize, ct));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<VenueTypeDto>> GetVenueTypeById(Guid id, CancellationToken ct)
    {
        var venueType = await _venueTypeService.GetVenueTypeByIdAsync(id, ct);
        if (venueType is null)
            return NotFound(new { message = $"VenueType with ID {id} not found" });

        return Ok(venueType);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<VenueTypeDto>> CreateVenueType(
        [FromBody] CreateVenueTypeRequest request,
        CancellationToken ct)
    {
        try
        {
            var venueType = await _venueTypeService.CreateVenueTypeAsync(request, ct);
            return CreatedAtAction(nameof(GetVenueTypeById), new { id = venueType.Id }, venueType);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<VenueTypeDto>> UpdateVenueType(
        Guid id,
        [FromBody] UpdateVenueTypeRequest request,
        CancellationToken ct)
    {
        try
        {
            var venueType = await _venueTypeService.UpdateVenueTypeAsync(id, request, ct);
            if (venueType is null)
                return NotFound(new { message = $"VenueType with ID {id} not found" });

            return Ok(venueType);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteVenueType(Guid id, CancellationToken ct)
    {
        var deleted = await _venueTypeService.DeleteVenueTypeAsync(id, ct);
        if (!deleted)
            return NotFound(new { message = $"VenueType with ID {id} not found" });

        return NoContent();
    }
}