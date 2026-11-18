using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AnalyticsController(IAnalyticsRepository analyticsRepository) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats() =>
        Ok(await analyticsRepository.GetAdminStatsAsync());
}
