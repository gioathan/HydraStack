using Hydra.Api.Contracts.Users;
using Hydra.Api.Services.Users;
using Microsoft.AspNetCore.Mvc;

namespace Hydra.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
	private readonly IUserService _userService;

	public UsersController(IUserService userService)
	{
		_userService = userService;
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
	public async Task<ActionResult<UserDto>> Register(
		[FromBody] CreateUserRequest request,
		CancellationToken ct)
	{
		try
		{
			var user = await _userService.CreateUserAsync(request, ct);
			return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost("login")]
	public async Task<ActionResult<UserDto>> Login(
		[FromBody] LoginRequest request,
		CancellationToken ct)
	{
		var user = await _userService.ValidateCredentialsAsync(request.Email, request.Password, ct);

		if (user is null)
			return Unauthorized(new { message = "Invalid email or password" });

		return Ok(user);
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