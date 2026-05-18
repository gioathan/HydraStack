using Hydra.Api.Contracts.Common;
using Hydra.Api.Contracts.Users;
using Hydra.Api.Services.Users;
using Hydra.Api.Services.Auth;
using Hydra.Api.Extensions;
using Hydra.Api.Auth;
using Hydra.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwt;
    private readonly IAuthEmailService _authEmail;

    public UsersController(IUserService userService, IJwtTokenService jwt, IAuthEmailService authEmail)
    {
        _userService = userService;
        _jwt = jwt;
        _authEmail = authEmail;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        return Ok(await _userService.GetAllUsersAsync(page, pageSize, ct));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Customer")]
    public async Task<ActionResult<UserDto>> GetUserById(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetUserByIdAsync(id, ct);
        if (user is null)
            return NotFound(new { message = $"User with ID {id} not found" });

        return Ok(user);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Customer")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult> UpdateUser(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        if (User.GetUserId() != id)
            return Forbid();

        var success = await _userService.UpdateUserPasswordAsync(id, request, ct);
        if (!success)
            return NotFound(new { message = $"User with ID {id} not found" });

        return Ok(new { message = "Password updated successfully" });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Customer")]
    public async Task<ActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        if (User.GetUserId() != id)
            return Forbid();

        var success = await _userService.DeleteUserAsync(id, ct);
        if (!success)
            return NotFound(new { message = $"User with ID {id} not found" });

        return NoContent();
    }

    [HttpPost("register/customer")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<CustomerAuthResponse>> CreateCustomer(
        [FromBody] RegisterCustomerRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _userService.RegisterCustomerWithProfileAsync(request);
            var token = _jwt.GenerateToken(
                result.User.Id,
                result.User.Email,
                UserRole.Customer,
                customerId: result.Customer.Id);

            await _authEmail.SendVerificationOtpAsync(result.User.Id, result.User.Email, ct);

            return Ok(new CustomerAuthResponse(result.User, result.Customer, token));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("register/venue")]
    [Authorize(Roles = "SuperAdmin")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<VenueAuthResponse>> CreateVenue(
        [FromBody] RegisterVenueRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _userService.RegisterVenueWithProfileAsync(request);
            var token = _jwt.GenerateToken(
                result.User.Id,
                result.User.Email,
                UserRole.Admin,
                venueId: result.Venue.Id);
            return Ok(new VenueAuthResponse(result.User, result.Venue, token));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}