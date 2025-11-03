using Asp.Versioning;
using Hydra.Api.Contracts.VenueTypes;
using Hydra.Api.Services.VenueTypes;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<List<VenueTypeDto>>> GetAllVenueTypes(CancellationToken ct)
    {
        var venueTypes = await _venueTypeService.GetAllVenueTypesAsync(ct);
        return Ok(venueTypes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VenueTypeDto>> GetVenueTypeById(Guid id, CancellationToken ct)
    {
        var venueType = await _venueTypeService.GetVenueTypeByIdAsync(id, ct);

        if (venueType is null)
            return NotFound(new { message = $"VenueType with ID {id} not found" });

        return Ok(venueType);
    }

    [HttpPost]
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
    public async Task<IActionResult> DeleteVenueType(Guid id, CancellationToken ct)
    {
        var deleted = await _venueTypeService.DeleteVenueTypeAsync(id, ct);

        if (!deleted)
            return NotFound(new { message = $"VenueType with ID {id} not found" });

        return NoContent();
    }
}