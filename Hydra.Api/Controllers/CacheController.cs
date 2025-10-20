using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Hydra.Api.Controllers;

public record CacheSetRequest(string Key, string Value, int TtlSeconds = 60);

[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly IConnectionMultiplexer _mux;
    public CacheController(IConnectionMultiplexer mux) => _mux = mux;

    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        var latency = await _mux.GetDatabase().PingAsync();
        return Ok(new { redis = $"ok ({latency.TotalMilliseconds:n0} ms)" });
    }

    [HttpPost("set")]
    public async Task<IActionResult> Set([FromBody] CacheSetRequest req)
    {
        var db = _mux.GetDatabase();
        await db.StringSetAsync(req.Key, req.Value, TimeSpan.FromSeconds(req.TtlSeconds));
        return Ok(new { set = req.Key, ttlSeconds = req.TtlSeconds });
    }

    [HttpGet("get")]
    public async Task<IActionResult> Get([FromQuery] string key)
    {
        var db = _mux.GetDatabase();
        var val = await db.StringGetAsync(key);
        return val.HasValue ? Ok(new { key, value = (string)val }) : NotFound(new { key });
    }

    [HttpGet("cache/test")]
    public async Task<IActionResult> Test(IConnectionMultiplexer mux)
    {
        var db = mux.GetDatabase();
        await db.StringSetAsync("hello", "world", TimeSpan.FromSeconds(60));
        var val = await db.StringGetAsync("hello");
        var ttl = await db.KeyTimeToLiveAsync("hello");
        return Ok(new { val = (string?)val, ttl = ttl?.TotalSeconds });
    }
}
