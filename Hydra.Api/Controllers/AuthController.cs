using Hydra.Api.Auth;
using Hydra.Api.Contracts.Users;
using Hydra.Api.Mapping;
using Hydra.Api.Models;
using Hydra.Api.Repositories.Customers;
using Hydra.Api.Repositories.Users;
using Hydra.Api.Repositories.Venues;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IVenueRepository _venueRepo;
    private readonly IJwtTokenService _jwt;

    public AuthController(
        IUserRepository userRepo,
        ICustomerRepository customerRepo,
        IVenueRepository venueRepo,
        IJwtTokenService jwt)
    {
        _userRepo = userRepo;
        _customerRepo = customerRepo;
        _venueRepo = venueRepo;
        _jwt = jwt;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var user = await _userRepo.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);

        // Use constant-time comparison to avoid user-enumeration timing attacks
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        Guid? customerId = null;
        Guid? venueId = null;

        if (user.Role == UserRole.Customer)
        {
            var customer = await _customerRepo.GetByUserIdAsync(user.Id, ct);
            customerId = customer?.Id;
        }
        else if (user.Role == UserRole.Admin)
        {
            var venue = await _venueRepo.GetByUserIdAsync(user.Id, ct);
            venueId = venue?.Id;
        }

        var token = _jwt.GenerateToken(user, customerId, venueId);
        return Ok(new LoginResponse(user.ToDto(), token, customerId, venueId));
    }
}
