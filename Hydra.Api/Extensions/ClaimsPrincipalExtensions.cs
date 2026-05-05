using System.Security.Claims;

namespace Hydra.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var val = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (val is null || !Guid.TryParse(val, out var id))
            throw new InvalidOperationException("User ID claim is missing or invalid.");
        return id;
    }

    public static string? GetRole(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value;

    public static Guid? GetCustomerId(this ClaimsPrincipal user)
    {
        var val = user.FindFirst("customerId")?.Value;
        return val is not null ? Guid.Parse(val) : null;
    }

    public static Guid? GetVenueId(this ClaimsPrincipal user)
    {
        var val = user.FindFirst("venueId")?.Value;
        return val is not null ? Guid.Parse(val) : null;
    }
}