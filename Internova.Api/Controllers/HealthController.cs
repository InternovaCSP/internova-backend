using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Controllers;

/// <summary>
/// Simple health-check controller to verify the API is alive.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    /// <summary>Returns a simple liveness ping.</summary>
    /// <returns>JSON object with status "ok".</returns>
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return Ok(new { status = "ok" });
    }
}
