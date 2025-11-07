using Hydra.Api.Contracts.Customers;
using Hydra.Api.Services.Customers;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetCustomers(
        [FromQuery] string? email,
        [FromQuery] string? phone,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var customer = await _customerService.GetCustomerByEmailAsync(email, ct);
            return customer is not null
                ? Ok(new List<CustomerDto> { customer })
                : Ok(new List<CustomerDto>());
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var customer = await _customerService.GetCustomerByPhoneAsync(phone, ct);
            return customer is not null
                ? Ok(new List<CustomerDto> { customer })
                : Ok(new List<CustomerDto>());
        }

        var customers = await _customerService.GetAllCustomersAsync(ct);
        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetCustomerById(Guid id, CancellationToken ct)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id, ct);

        if (customer is null)
            return NotFound(new { message = $"Customer with ID {id} not found" });

        return Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken ct)
    {
        try
        {
            var customer = await _customerService.CreateCustomerAsync(request, ct);
            return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, customer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(
        Guid id,
        [FromBody] CreateCustomerRequest request,
        CancellationToken ct)
    {
        try
        {
            var customer = await _customerService.UpdateCustomerAsync(id, request, ct);

            if (customer is null)
                return NotFound(new { message = $"Customer with ID {id} not found" });

            return Ok(customer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCustomer(Guid id, CancellationToken ct)
    {
        var deleted = await _customerService.DeleteCustomerAsync(id, ct);

        if (!deleted)
            return NotFound(new { message = $"Customer with ID {id} not found" });

        return NoContent();
    }
}