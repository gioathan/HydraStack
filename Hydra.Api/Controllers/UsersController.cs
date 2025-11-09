using Hydra.Api.Contracts.Users;
using Hydra.Api.Contracts.Customers;
using Hydra.Api.Services.Users;
using Hydra.Api.Services.Customers;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Hydra.Api.Auth;
using Hydra.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
	private readonly IUserService _userService;
    private readonly ICustomerService _customerService;
    private readonly IJwtTokenService _jwt;

    public UsersController(IUserService userService, ICustomerService customerService, IJwtTokenService jwt)
	{
		_userService = userService;
        _customerService = customerService;
        _jwt = jwt;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers(CancellationToken ct)
	{
		var users = await _userService.GetAllUsersAsync(ct);
		return Ok(users);
	}

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Customer")]
    public async Task<ActionResult<UserDto>> GetUserById(Guid id, CancellationToken ct)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if ((userRole == "Customer" || userRole == "Admin") && currentUserId != id.ToString())
        {
            return Forbid();
        }

        var user = await _userService.GetUserByIdAsync(id, ct);
        if (user is null)
            return NotFound(new { message = $"User with ID {id} not found" });

        return Ok(user);
    }

    [HttpPost("register")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<AuthResponse>> RegisterByAdmin(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        try
        {
            var user = await _userService.CreateUserAsync(request, ct);

            var token = _jwt.GenerateToken(
                user.Id,
                user.Email,
                Enum.Parse<UserRole>(user.Role, ignoreCase: true));

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new AuthResponse(user, token));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("registerUser")]
    public async Task<ActionResult<AuthResponse>> RegisterPublic(
            [FromBody] CreateUserRequest request,
            CancellationToken ct)
    {
        try
        {
            var user = await _userService.CreateUserAsync(request, ct);

            var token = _jwt.GenerateToken(
                user.Id,
                user.Email,
                UserRole.Customer);

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new AuthResponse(user, token));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var user = await _userService.ValidateCredentialsAsync(request.Email, request.Password, ct);
        if (user is null)
            return Unauthorized(new { message = "Invalid email or password" });

        var token = _jwt.GenerateToken(
            user.Id,
            user.Email,
            Enum.Parse<UserRole>(user.Role, ignoreCase: true));

        return Ok(new AuthResponse(user, token));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
	{
		var deleted = await _userService.DeleteUserAsync(id, ct);

		if (!deleted)
			return NotFound(new { message = $"User with ID {id} not found" });

		return NoContent();
	}

    [HttpGet("{id:guid}/customer")]
    [Authorize(Roles = "SuperAdmin,Admin,Customer")]
    public async Task<ActionResult<CustomerDto>> GetCustomerByUserId(Guid id, CancellationToken ct)
    {
        var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "Customer")
        {
            if (string.IsNullOrEmpty(currentUserIdString) ||
                !Guid.TryParse(currentUserIdString, out var currentUserId) ||
                currentUserId != id)
            {
                return Forbid();
            }
        }

        var customer = await _customerService.GetCustomerByUserIdAsync(id, ct);

        if (customer is null)
            return NotFound(new { message = $"Customer with User ID {id} not found" });

        return Ok(customer);
    }

    [HttpPut("{id:guid}/customer")]
    [Authorize(Roles = "SuperAdmin,Customer")]
    public async Task<ActionResult<CustomerDto>> UpdateCustomerByUserId(
    Guid id,
    [FromBody] CreateCustomerRequest request,
    CancellationToken ct)
    {
        var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "Customer")
        {
            if (string.IsNullOrEmpty(currentUserIdString) ||
                !Guid.TryParse(currentUserIdString, out var currentUserId) ||
                currentUserId != id)
            {
                return Forbid();
            }
        }

        try
        {
            var customer = await _customerService.UpdateCustomerByUserIdAsync(id, request, ct);

            if (customer is null)
                return NotFound(new { message = $"Customer with User ID {id} not found" });

            return Ok(customer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/customer")]
    [Authorize(Roles = "SuperAdmin,Customer")]
    public async Task<IActionResult> DeleteByUserIdCustomer(Guid id, CancellationToken ct)
    {
        var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "Customer")
        {
            if (string.IsNullOrEmpty(currentUserIdString) ||
                !Guid.TryParse(currentUserIdString, out var currentUserId) ||
                currentUserId != id)
            {
                return Forbid();
            }
        }

        var deleted = await _customerService.DeleteCustomerByUserIdAsync(id, ct);

        if (!deleted)
            return NotFound(new { message = $"Customer with User ID {id} not found" });

        return NoContent();
    }
}

public record LoginRequest(string Email, string Password);