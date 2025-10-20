using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StackExchange.Redis;

namespace Hydra.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly NpgsqlDataSource _ds;
    private readonly IConnectionMultiplexer _mux;

    public HealthController(NpgsqlDataSource ds, IConnectionMultiplexer mux)
    {
        _ds = ds; _mux = mux;
    }

    [HttpGet]
    public IActionResult Get() => Ok(new { ok = true, utc = DateTime.UtcNow });

    [HttpGet("deep")]
    public async Task<IActionResult> Deep()
    {
        await using var conn = await _ds.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT 1;", conn);
        var okPg = (int?)await cmd.ExecuteScalarAsync() == 1;

        var latency = await _mux.GetDatabase().PingAsync();

        return Ok(new
        {
            postgres = okPg ? "ok" : "fail",
            redis = $"ok ({latency.TotalMilliseconds:n0} ms)"
        });
    }
}
