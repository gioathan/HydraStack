using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Customers;
using Hydra.Api.Repositories.Customers;
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
    private readonly ICustomerRepository _customerRepo;

    public CustomersController(ICustomerService customerService, ICustomerRepository customerRepo)
    {
        _customerService = customerService;
        _customerRepo = customerRepo;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetCustomers(
        [FromQuery] string? email,
        [FromQuery] string? phone,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            email = email.Trim();
            if (email.Length > 256)
                return BadRequest(new { message = "Email must not exceed 256 characters." });

            var customer = await _customerService.GetCustomerByEmailAsync(email, ct);
            var items = customer is not null ? new List<CustomerDto> { customer } : new List<CustomerDto>();
            return Ok(new PagedResult<CustomerDto>(items, items.Count, 1, items.Count == 0 ? 1 : items.Count));
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            phone = phone.Trim();
            if (phone.Length > 30)
                return BadRequest(new { message = "Phone must not exceed 30 characters." });

            var customer = await _customerService.GetCustomerByPhoneAsync(phone, ct);
            var items = customer is not null ? new List<CustomerDto> { customer } : new List<CustomerDto>();
            return Ok(new PagedResult<CustomerDto>(items, items.Count, 1, items.Count == 0 ? 1 : items.Count));
        }

        return Ok(await _customerService.GetAllCustomersAsync(page, pageSize, ct));
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

    [HttpPut("{id:guid}/push-token")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> RegisterPushToken(
        Guid id,
        [FromBody] RegisterPushTokenRequest request,
        CancellationToken ct)
    {
        if (User.GetCustomerId() != id)
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.PushToken))
            return BadRequest(new { message = "Push token is required." });

        if (!request.PushToken.StartsWith("ExponentPushToken[") || !request.PushToken.EndsWith("]"))
            return BadRequest(new { message = "Invalid Expo push token format." });

        await _customerRepo.UpdatePushTokenAsync(id, request.PushToken, ct);
        return Ok(new { message = "Push token registered." });
    }

    [HttpDelete("{id:guid}/push-token")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> RemovePushToken(Guid id, CancellationToken ct)
    {
        if (User.GetCustomerId() != id)
            return Forbid();

        await _customerRepo.UpdatePushTokenAsync(id, null, ct);
        return NoContent();
    }
}
