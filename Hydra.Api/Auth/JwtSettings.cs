namespace Hydra.Api.Auth;

public class JwtSettings
{
	public string Secret { get; set; } = default!;
	public string Issuer { get; set; } = default!;
	public string Audience { get; set; } = default!;
	public int ExpirationHours { get; set; } = 4;
}