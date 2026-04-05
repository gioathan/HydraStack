using Hydra.Api.Contracts.Customers;
using Hydra.Api.Services.Customers;
using Hydra.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;

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
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<List<CustomerDto>>> GetCustomers(
        [FromQuery] string? email,
        [FromQuery] string? phone,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var customer = await _customerService.GetCustomerByEmailAsync(email, ct);
            return Ok(customer is not null ? new List<CustomerDto> { customer } : new List<CustomerDto>());
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var customer = await _customerService.GetCustomerByPhoneAsync(phone, ct);
            return Ok(customer is not null ? new List<CustomerDto> { customer } : new List<CustomerDto>());
        }

        return Ok(await _customerService.GetAllCustomersAsync(ct));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Customer")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(Guid id, CancellationToken ct)
    {
        if (User.GetRole() == "Customer" && User.GetCustomerId() != id)
            return Forbid();

        var customer = await _customerService.GetCustomerByIdAsync(id, ct);
        if (customer is null)
            return NotFound(new { message = $"Customer with ID {id} not found" });

        return Ok(customer);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Customer")]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken ct)
    {
        if (User.GetRole() == "Customer" && User.GetCustomerId() != id)
            return Forbid();

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
}