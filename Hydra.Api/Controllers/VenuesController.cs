using Hydra.Api.Caching;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Data;
using Hydra.Api.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenuesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICache _cache;

    private static readonly TimeSpan ListTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DetailTtl = TimeSpan.FromMinutes(20);

    public VenuesController(AppDbContext db, ICache cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <summary>
    /// Create a new venue
    /// POST /api/venues
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateVenueRequest request,
        CancellationToken ct)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required." });
        }

        // DTO → Model using mapping extension
        var venue = request.ToModel();

        _db.Venues.Add(venue);
        await _db.SaveChangesAsync(ct);

        // Invalidate venue caches
        await _cache.BumpTokenAsync(CacheKeys.VenuesToken);

        // Model → DTO using mapping extension
        return CreatedAtAction(
            nameof(Get),
            new { id = venue.Id },
            venue.ToDto());
    }

    /// <summary>
    /// Get a venue by ID (cached)
    /// GET /api/venues/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VenueDto>> Get(Guid id, CancellationToken ct)
    {
        var version = await _cache.GetTokenAsync(CacheKeys.VenuesToken);
        var cacheKey = CacheKeys.VenueDetail(id, version);

        var dto = await _cache.GetOrSetAsync(
            cacheKey,
            ttl: DetailTtl,
            factory: async _ =>
            {
                var venue = await _db.Venues
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct);

                // Model → DTO using mapping extension
                return venue?.ToDto();
            },
            jitter: TimeSpan.FromSeconds(20),
            ct: ct);

        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// List all venues (cached)
    /// GET /api/venues
    /// </summary>
    /// <remarks>
    /// Simple MVP: returns all venues, cached. Add paging/search later.
    /// </remarks>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VenueDto>>> List(CancellationToken ct)
    {
        var version = await _cache.GetTokenAsync(CacheKeys.VenuesToken);
        var cacheKey = CacheKeys.VenuesList(version);

        var list = await _cache.GetOrSetAsync(
            cacheKey,
            ttl: ListTtl,
            factory: async _ =>
            {
                var venues = await _db.Venues
                    .AsNoTracking()
                    .OrderBy(v => v.Name)
                    .ToListAsync(ct);

                // Model → DTO using mapping extension
                return venues.Select(v => v.ToDto()).ToList();
            },
            jitter: TimeSpan.FromSeconds(30),
            ct: ct);

        return Ok(list);
    }

    /// <summary>
    /// Update an existing venue
    /// PUT /api/venues/{id}
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVenueRequest request,
        CancellationToken ct)
    {
        var venue = await _db.Venues.FindAsync([id], ct);

        if (venue is null)
            return NotFound();

        // DTO → Model update using mapping extension
        venue.UpdateFrom(request);

        await _db.SaveChangesAsync(ct);

        // Invalidate venue caches
        await _cache.BumpTokenAsync(CacheKeys.VenuesToken);

        return NoContent();
    }

    /// <summary>
    /// Delete a venue
    /// DELETE /api/venues/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var venue = await _db.Venues.FindAsync([id], ct);

        if (venue is null)
            return NotFound();

        _db.Venues.Remove(venue);
        await _db.SaveChangesAsync(ct);

        // Invalidate venue caches
        await _cache.BumpTokenAsync(CacheKeys.VenuesToken);

        return NoContent();
    }
}