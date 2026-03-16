using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Internova.Api.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController(
    IInternshipApplicationRepository applicationRepository,
    ILogger<ApplicationsController> logger) : ControllerBase
{
    // ─── GET /api/applications/pipeline-stats ────────────────────────────────
    [HttpGet("pipeline-stats")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetPipelineStats()
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new { error = "Student identity could not be determined." });
        }

        try
        {
            var stats = await applicationRepository.GetPipelineStatsAsync(studentId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch pipeline stats for student {StudentId}", studentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
