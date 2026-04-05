using System.Security.Claims;

namespace Hydra.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

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