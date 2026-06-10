using Hydra.Api.Contracts.Venues;
using Hydra.Api.Extensions;
using Hydra.Api.Services.Venues;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/venues/{venueId:guid}/events")]
public class VenueEventsController : ControllerBase
{
    private readonly IVenueEventService _eventService;
    private readonly IVenueService _venueService;

    public VenueEventsController(IVenueEventService eventService, IVenueService venueService)
    {
        _eventService = eventService;
        _venueService = venueService;
    }

    [HttpGet]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    public async Task<ActionResult<IReadOnlyList<VenueEventDto>>> GetEvents(
        Guid venueId,
        [FromQuery] bool includePast = false,
        CancellationToken ct = default)
    {
        return Ok(await _eventService.GetEventsAsync(venueId, includePast, ct));
    }

    [HttpGet("{eventId:guid}")]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    public async Task<ActionResult<VenueEventDto>> GetEvent(Guid venueId, Guid eventId, CancellationToken ct)
    {
        var ev = await _eventService.GetEventByIdAsync(venueId, eventId, ct);
        if (ev is null) return NotFound(new { message = $"Event {eventId} not found." });
        return Ok(ev);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<VenueEventDto>> CreateEvent(
        Guid venueId,
        [FromBody] CreateVenueEventRequest request,
        CancellationToken ct)
    {
        if (!await AdminOwnsVenueOrIsSuperAdmin(venueId, ct))
            return Forbid();

        var (result, error) = await _eventService.CreateEventAsync(venueId, request, ct);
        if (error is not null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetEvent), new { venueId, eventId = result!.Id }, result);
    }

    [HttpPut("{eventId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<VenueEventDto>> UpdateEvent(
        Guid venueId,
        Guid eventId,
        [FromBody] UpdateVenueEventRequest request,
        CancellationToken ct)
    {
        if (!await AdminOwnsVenueOrIsSuperAdmin(venueId, ct))
            return Forbid();

        var (result, error) = await _eventService.UpdateEventAsync(venueId, eventId, request, ct);
        if (error is not null) return BadRequest(new { message = error });
        if (result is null) return NotFound(new { message = $"Event {eventId} not found." });
        return Ok(result);
    }

    [HttpDelete("{eventId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteEvent(Guid venueId, Guid eventId, CancellationToken ct)
    {
        if (!await AdminOwnsVenueOrIsSuperAdmin(venueId, ct))
            return Forbid();

        var deleted = await _eventService.DeleteEventAsync(venueId, eventId, ct);
        if (!deleted) return NotFound(new { message = $"Event {eventId} not found." });
        return NoContent();
    }

    [HttpPost("{eventId:guid}/close")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<VenueEventDto>> CloseEvent(Guid venueId, Guid eventId, CancellationToken ct)
    {
        if (!await AdminOwnsVenueOrIsSuperAdmin(venueId, ct))
            return Forbid();

        var ev = await _eventService.CloseEventAsync(venueId, eventId, ct);
        if (ev is null) return NotFound(new { message = $"Event {eventId} not found." });
        return Ok(ev);
    }

    [HttpPost("{eventId:guid}/photos")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<VenueEventDto>> AddPhoto(
        Guid venueId,
        Guid eventId,
        [FromBody] AddEventPhotoRequest request,
        CancellationToken ct)
    {
        if (!await AdminOwnsVenueOrIsSuperAdmin(venueId, ct))
            return Forbid();

        var ev = await _eventService.AddEventPhotoAsync(venueId, eventId, request, ct);
        if (ev is null) return NotFound(new { message = $"Event {eventId} not found." });
        return Ok(ev);
    }

    [HttpDelete("{eventId:guid}/photos/{photoId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeletePhoto(Guid venueId, Guid eventId, Guid photoId, CancellationToken ct)
    {
        if (!await AdminOwnsVenueOrIsSuperAdmin(venueId, ct))
            return Forbid();

        var deleted = await _eventService.DeleteEventPhotoAsync(venueId, eventId, photoId, ct);
        if (!deleted) return NotFound(new { message = $"Photo {photoId} not found." });
        return NoContent();
    }

    private async Task<bool> AdminOwnsVenueOrIsSuperAdmin(Guid venueId, CancellationToken ct)
    {
        if (User.GetRole() == "SuperAdmin") return true;
        var venue = await _venueService.GetVenueByIdAsync(venueId, ct);
        return venue is not null && venue.UserId == User.GetUserId();
    }
}
