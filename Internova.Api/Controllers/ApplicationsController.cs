using Internova.Core.Interfaces;
using Internova.Core.Entities;
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

    // ─── GET /api/applications/company ─────────────────────────────────────
    [HttpGet("company")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> GetCompanyApplications()
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var companyId))
        {
            return Unauthorized(new { error = "Company identity could not be determined." });
        }

        try
        {
            var applications = await applicationRepository.GetByCompanyIdAsync(companyId);
            return Ok(applications);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch applications for company {CompanyId}", companyId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ─── PATCH /api/applications/{id}/status ────────────────────────────────
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            if (!Enum.TryParse<Internova.Core.Enums.ApplicationStatus>(request.Status, out var status))
            {
                return BadRequest(new { error = "Invalid status." });
            }

            var success = await applicationRepository.UpdateStatusAsync(id, status);
            if (!success) return NotFound(new { error = "Application not found." });

            return Ok(new { message = "Status updated successfully." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update status for application {ApplicationId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ─── POST /api/applications/apply ──────────────────────────────────────
    [HttpPost("apply")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Apply([FromBody] ApplyRequest request)
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new { error = "Student identity could not be determined." });
        }

        try
        {
            // Check if already applied
            var existing = await applicationRepository.GetByStudentIdAsync(studentId);
            if (existing.Any(a => a.InternshipId == request.InternshipId))
            {
                return BadRequest(new { error = "You have already applied for this internship." });
            }

            var application = new InternshipApplication
            {
                InternshipId = request.InternshipId,
                StudentId = studentId,
                Status = Internova.Core.Enums.ApplicationStatus.Applied,
                AppliedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await applicationRepository.AddAsync(application);
            return Ok(new { message = "Application submitted successfully!" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to submit application for student {StudentId}", studentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ─── GET /api/applications/student ─────────────────────────────────────
    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentApplications()
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new { error = "Student identity could not be determined." });
        }

        try
        {
            var applications = await applicationRepository.GetByStudentIdAsync(studentId);
            return Ok(applications);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch applications for student {StudentId}", studentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ─── GET /api/applications/kpi-stats ───────────────────────────────────
    [HttpGet("kpi-stats")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetKpiStats()
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new { error = "Student identity could not be determined." });
        }

        try
        {
            var stats = await applicationRepository.GetKpiStatsAsync(studentId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch KPI stats for student {StudentId}", studentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    public class ApplyRequest
    {
        public int InternshipId { get; set; }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
