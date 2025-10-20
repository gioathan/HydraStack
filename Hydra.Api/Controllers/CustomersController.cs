using Hydra.Api.Contracts.Customers;
using Hydra.Api.Data;
using Hydra.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomersController(AppDbContext db) => _db = db;

    // POST /api/customers
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest req, CancellationToken ct)
    {
        // MVP: allow null email/phone, but at least one would be nice
        if (string.IsNullOrWhiteSpace(req.Email) && string.IsNullOrWhiteSpace(req.Phone))
            return BadRequest("Provide at least email or phone.");

        var c = new Customer
        {
            Email = req.Email?.Trim(),
            Phone = req.Phone?.Trim(),
            Locale = string.IsNullOrWhiteSpace(req.Locale) ? "en" : req.Locale,
            MarketingOptIn = req.MarketingOptIn
        };
        _db.Customers.Add(c);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = c.Id }, ToDto(c));
    }

    // GET /api/customers/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await _db.Customers.AsNoTracking()
                                     .Where(x => x.Id == id)
                                     .Select(x => ToDto(x))
                                     .FirstOrDefaultAsync(ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    // GET /api/customers?email=&phone=
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> List(string? email, string? phone, CancellationToken ct)
    {
        var q = _db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(email))
            q = q.Where(c => c.Email != null && c.Email.ToLower() == email.ToLower());
        if (!string.IsNullOrWhiteSpace(phone))
            q = q.Where(c => c.Phone != null && c.Phone == phone);

        var list = await q.OrderByDescending(c => c.CreatedAtUtc)
                          .Take(100)
                          .Select(c => ToDto(c))
                          .ToListAsync(ct);
        return Ok(list);
    }

    private static CustomerDto ToDto(Customer c) =>
        new(c.Id, c.Email, c.Phone, c.Locale, c.MarketingOptIn, c.CreatedAtUtc, null);
}
