using Hydra.Api.Contracts.Customers;
using Hydra.Api.Data;
using Hydra.Api.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db) => _db = db;

    /// <summary>
    /// Create a new customer
    /// POST /api/customers
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken ct)
    {
        // Validation: At least one contact method required
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
        {
            return BadRequest(new { error = "Provide at least email or phone." });
        }

        // DTO → Model using mapping extension
        var customer = request.ToModel();

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        // Model → DTO using mapping extension
        return CreatedAtAction(
            nameof(Get),
            new { id = customer.Id },
            customer.ToDto());
    }

    /// <summary>
    /// Get a customer by ID
    /// GET /api/customers/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken ct)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (customer is null)
            return NotFound();

        // Model → DTO using mapping extension
        return Ok(customer.ToDto());
    }

    /// <summary>
    /// Search customers by email or phone
    /// GET /api/customers?email=test@example.com&phone=555-1234
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> List(
        [FromQuery] string? email,
        [FromQuery] string? phone,
        CancellationToken ct)
    {
        var query = _db.Customers.AsNoTracking();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailLower = email.ToLower().Trim();
            query = query.Where(c => c.Email != null && c.Email.ToLower() == emailLower);
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var phoneTrimmed = phone.Trim();
            query = query.Where(c => c.Phone != null && c.Phone == phoneTrimmed);
        }

        // Execute query with mapping
        var customers = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(100)
            .ToListAsync(ct);

        // Model → DTO using mapping extension
        var dtos = customers.Select(c => c.ToDto()).ToList();

        return Ok(dtos);
    }
}