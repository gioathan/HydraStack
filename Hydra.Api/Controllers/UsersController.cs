using Hydra.Api.Contracts.Users;
using Hydra.Api.Services.Users;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Hydra.Api.Auth;
using Hydra.Api.Models;

namespace Hydra.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
	private readonly IUserService _userService;
    private readonly IJwtTokenService _jwt;

    public UsersController(IUserService userService, IJwtTokenService jwt)
	{
		_userService = userService;
        _jwt = jwt;
    }

    [HttpGet]
	public async Task<ActionResult<List<UserDto>>> GetAllUsers(CancellationToken ct)
	{
		var users = await _userService.GetAllUsersAsync(ct);
		return Ok(users);
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<UserDto>> GetUserById(Guid id, CancellationToken ct)
	{
		var user = await _userService.GetUserByIdAsync(id, ct);

		if (user is null)
			return NotFound(new { message = $"User with ID {id} not found" });

		return Ok(user);
	}

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
            [FromBody] CreateUserRequest request,
            CancellationToken ct)
    {
        try
        {
            var user = await _userService.CreateUserAsync(request, ct);

            // Build token using the dto fields
            var token = _jwt.GenerateToken(
                user.Id,
                user.Email,
                Enum.Parse<UserRole>(user.Role, ignoreCase: true)); // dto.Role is string

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
	public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
	{
		var deleted = await _userService.DeleteUserAsync(id, ct);

		if (!deleted)
			return NotFound(new { message = $"User with ID {id} not found" });

		return NoContent();
	}
}

public record LoginRequest(string Email, string Password);