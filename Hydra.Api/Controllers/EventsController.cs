using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Services.Venues;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/events")]
public class EventsController : ControllerBase
{
    private readonly IVenueEventService _eventService;

    public EventsController(IVenueEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    public async Task<ActionResult<PagedResult<EventListItemDto>>> GetUpcomingEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? location = null,
        CancellationToken ct = default)
    {
        return Ok(await _eventService.GetUpcomingPagedAsync(page, pageSize, location, ct));
    }
}
