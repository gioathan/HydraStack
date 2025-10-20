using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Hydra.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DbController : ControllerBase
{
    private readonly NpgsqlDataSource _ds;
    public DbController(NpgsqlDataSource ds) => _ds = ds;

    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        await using var conn = await _ds.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT 1;", conn);
        var result = (int?)await cmd.ExecuteScalarAsync();
        return Ok(new { postgres = result == 1 ? "ok" : "unexpected" });
    }

    [HttpGet("info")]
    public async Task<IActionResult> Info()
    {
        await using var conn = await _ds.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("select version(), current_database(), now()", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();
        return Ok(new { version = r.GetString(0), database = r.GetString(1), serverTime = r.GetDateTime(2) });
    }

    [HttpGet("tables")]
    public async Task<IActionResult> Tables()
    {
        await using var conn = await _ds.OpenConnectionAsync();
        const string sql = @"SELECT table_name
                         FROM information_schema.tables
                         WHERE table_schema='public'
                         ORDER BY table_name";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync();
        var list = new List<string>();
        while (await r.ReadAsync())
            list.Add(r.GetString(0));
        return Ok(list);
    }


}
