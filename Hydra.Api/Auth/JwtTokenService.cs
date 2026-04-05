using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hydra.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Hydra.Api.Auth;

public interface IJwtTokenService
{
    string GenerateToken(User user, Guid? customerId = null, Guid? venueId = null);
    string GenerateToken(Guid userId, string email, UserRole role, Guid? customerId = null, Guid? venueId = null);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(Guid userId, string email, UserRole role, Guid? customerId = null, Guid? venueId = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, userId.ToString()),
        new(JwtRegisteredClaimNames.Email, email),
        new(ClaimTypes.NameIdentifier, userId.ToString()),
        new(ClaimTypes.Role, role.ToString()),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        if (customerId.HasValue)
            claims.Add(new Claim("customerId", customerId.Value.ToString()));

        if (venueId.HasValue)
            claims.Add(new Claim("venueId", venueId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateToken(User user, Guid? customerId = null, Guid? venueId = null) =>
        GenerateToken(user.Id, user.Email, user.Role, customerId, venueId);
}