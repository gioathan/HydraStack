using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hydra.Api.Controllers;

// Temporary — used to verify the Sentry pipeline end-to-end, then removed.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/debug")]
[AllowAnonymous]
public class DebugController : ControllerBase
{
    [HttpGet("test-error")]
    public IActionResult TestError()
    {
        throw new InvalidOperationException("Sentry test error — safe to ignore.");
    }
}
