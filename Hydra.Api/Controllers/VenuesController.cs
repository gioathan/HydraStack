using Hydra.Api.Caching;
using Hydra.Api.Contracts.Venues;
using Hydra.Api.Data;
using Hydra.Api.Models;
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
        _db = db; _cache = cache;
    }

    // POST /api/venues
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVenueRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");

        var v = new Venue { Name = req.Name, Address = req.Address, Capacity = req.Capacity };
        _db.Venues.Add(v);
        await _db.SaveChangesAsync(ct);

        // Invalidate venues caches
        await _cache.BumpTokenAsync(CacheKeys.VenuesToken);

        return CreatedAtAction(nameof(Get), new { id = v.Id }, ToDto(v));
    }

    // GET /api/venues/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VenueDto>> Get(Guid id, CancellationToken ct)
    {
        var ver = await _cache.GetTokenAsync(CacheKeys.VenuesToken);
        var key = CacheKeys.VenueDetail(id, ver);

        var dto = await _cache.GetOrSetAsync(
            key,
            ttl: DetailTtl,
            factory: async _ => await _db.Venues.AsNoTracking()
                                                .Where(x => x.Id == id)
                                                .Select(x => ToDto(x))
                                                .FirstOrDefaultAsync(ct),
            jitter: TimeSpan.FromSeconds(20),
            ct: ct);

        return dto is null ? NotFound() : Ok(dto);
    }

    // GET /api/venues
    // Simple MVP: return all, cached. (Add paging/search later.)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VenueDto>>> List(CancellationToken ct)
    {
        var ver = await _cache.GetTokenAsync(CacheKeys.VenuesToken);
        var key = CacheKeys.VenuesList(ver);

        var list = await _cache.GetOrSetAsync(
            key,
            ttl: ListTtl,
            factory: async _ => await _db.Venues.AsNoTracking()
                                                .OrderBy(v => v.Name)
                                                .Select(v => ToDto(v))
                                                .ToListAsync(ct),
            jitter: TimeSpan.FromSeconds(30),
            ct: ct);

        return Ok(list);
    }

    // PUT /api/venues/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVenueRequest req, CancellationToken ct)
    {
        var v = await _db.Venues.FindAsync([id], ct);
        if (v is null) return NotFound();

        v.Name = req.Name;
        v.Address = req.Address;
        v.Capacity = req.Capacity;
        await _db.SaveChangesAsync(ct);

        await _cache.BumpTokenAsync(CacheKeys.VenuesToken);
        return NoContent();
    }

    // DELETE /api/venues/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var v = await _db.Venues.FindAsync([id], ct);
        if (v is null) return NotFound();

        _db.Venues.Remove(v);
        await _db.SaveChangesAsync(ct);

        await _cache.BumpTokenAsync(CacheKeys.VenuesToken);
        return NoContent();
    }

    private static VenueDto ToDto(Venue v) => new(v.Id, v.Name, v.Address, v.Capacity);
}
