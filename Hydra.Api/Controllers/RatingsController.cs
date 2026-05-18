using Hydra.Api.Contracts.Ratings;
using Hydra.Api.Services.Ratings;
using Hydra.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<IReadOnlyList<PendingRatingDto>>> GetPendingRatings(CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
            return Forbid();

        var pending = await _ratingService.GetPendingRatingsAsync(customerId.Value, ct);
        return Ok(pending);
    }
}
