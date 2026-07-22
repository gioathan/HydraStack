using Asp.Versioning;
using Google.Apis.Auth;
using Hydra.Api.Auth;
using Hydra.Api.Caching;
using Hydra.Api.Configuration;
using Hydra.Api.Contracts.Auth;
using Hydra.Api.Contracts.Users;
using Hydra.Api.Mapping;
using Hydra.Api.Models;
using Hydra.Api.Repositories.Customers;
using Hydra.Api.Repositories.Users;
using Hydra.Api.Repositories.Venues;
using Hydra.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

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
    private readonly IAuthEmailService _authEmail;
    private readonly GoogleAuthSettings _googleAuth;

    public AuthController(
        IUserRepository userRepo,
        ICustomerRepository customerRepo,
        IVenueRepository venueRepo,
        IJwtTokenService jwt,
        IAuthEmailService authEmail,
        IOptions<GoogleAuthSettings> googleAuth)
    {
        _userRepo = userRepo;
        _customerRepo = customerRepo;
        _venueRepo = venueRepo;
        _jwt = jwt;
        _authEmail = authEmail;
        _googleAuth = googleAuth.Value;
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

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            if (user?.AuthProvider == AuthProvider.Google)
                return Unauthorized(new { message = "This account uses Google Sign-In. Please log in with Google." });

            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (!user.IsEmailVerified && user.Role == UserRole.Customer)
            return StatusCode(403, new { message = "Please verify your email before logging in." });

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

    [HttpPost("google")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> GoogleLogin(
        [FromBody] GoogleAuthRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequest(new { message = "ID token is required." });

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_googleAuth.ClientId]
                });
        }
        catch
        {
            return Unauthorized(new { message = "Invalid Google token." });
        }

        var email = payload.Email.Trim().ToLowerInvariant();
        var user = await _userRepo.GetByEmailAsync(email, ct);

        if (user is not null && user.AuthProvider != AuthProvider.Google)
            return Unauthorized(new { message = "This email is already registered with a password. Please sign in with your email and password." });

        if (user is null)
        {
            user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                Role = UserRole.Customer,
                IsEmailVerified = true,
                AuthProvider = AuthProvider.Google
            };
            await _userRepo.AddAsync(user, ct);

            await _customerRepo.AddAsync(new Customer
            {
                UserId = user.Id,
                Name = payload.Name,
                Email = email,
                Locale = "en",
                CreatedAtUtc = DateTime.UtcNow
            }, ct);
        }

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

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Code is required." });

        var user = await _userRepo.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return BadRequest(new { message = "Invalid request." });

        if (user.IsEmailVerified)
            return Ok(new { message = "Email is already verified." });

        var valid = await _authEmail.VerifyAndConsumeEmailOtpAsync(request.UserId, request.Code, ct);
        if (!valid)
            return BadRequest(new { message = "Invalid or expired verification code." });

        user.IsEmailVerified = true;
        await _userRepo.UpdateAsync(user, ct);

        return Ok(new { message = "Email verified successfully." });
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequest request,
        CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return BadRequest(new { message = "Invalid request." });

        if (user.IsEmailVerified)
            return Ok(new { message = "Email is already verified." });

        var sent = await _authEmail.ResendVerificationOtpAsync(request.UserId, user.Email, ct);
        if (!sent)
            return StatusCode(429, new { message = "Too many resend attempts. Please wait before trying again." });

        return Ok(new { message = "Verification code sent." });
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required." });

        var user = await _userRepo.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);

        if (user is not null)
            await _authEmail.SendPasswordResetOtpAsync(user.Id, user.Email, ct);

        return Ok(new { message = "If an account with that email exists, a reset code has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Email, code, and new password are required." });

        try
        {
            ValidatePassword(request.NewPassword);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var user = await _userRepo.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user is null)
            return BadRequest(new { message = "Invalid or expired reset code." });

        var valid = await _authEmail.VerifyAndConsumePasswordResetOtpAsync(user.Id, request.Code, ct);
        if (!valid)
            return BadRequest(new { message = "Invalid or expired reset code." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepo.UpdateAsync(user, ct);

        return Ok(new { message = "Password reset successfully." });
    }

    private static void ValidatePassword(string password)
    {
        if (password.Length < 10)
            throw new InvalidOperationException("Password must be at least 10 characters long.");
        if (!password.Any(char.IsUpper))
            throw new InvalidOperationException("Password must contain at least one uppercase letter.");
        if (!password.Any(char.IsDigit))
            throw new InvalidOperationException("Password must contain at least one digit.");
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            throw new InvalidOperationException("Password must contain at least one special character.");
    }
}
