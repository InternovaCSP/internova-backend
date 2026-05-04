using Internova.Core.Interfaces;
using Internova.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Internova.Api.Controllers;

/// <summary>
/// Manages internship applications.
/// Students can apply to internships and view their application status.
/// Companies can view applications for their postings and update status.
/// </summary>
[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController(
    IInternshipApplicationRepository applicationRepository,
    IStudentProfileRepository studentProfileRepository,
    ILogger<ApplicationsController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves a student's profile by their user ID.
    /// Accessible to Companies and Admins only.
    /// </summary>
    /// <param name="studentId">The ID of the student user.</param>
    /// <returns>The student profile or 404 Not Found.</returns>
    [HttpGet("student/{studentId:int}/profile")]
    [Authorize(Roles = "Company,Admin")]
    public async Task<IActionResult> GetStudentProfile(int studentId)
    {
        // (Implementation remains unchanged)
        try
        {
            var profile = await studentProfileRepository.GetByUserIdAsync(studentId);
            if (profile == null) return NotFound(new { error = "Student profile not found." });
            return Ok(profile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch profile for student {StudentId}", studentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Retrieves application pipeline statistics for the authenticated student.
    /// </summary>
    /// <returns>Statistics about application stages.</returns>
    [HttpGet("pipeline-stats")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetPipelineStats()
    {
        // ... (remaining methods will be updated similarly)
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

    /// <summary>
    /// Retrieves all applications received by the authenticated company.
    /// </summary>
    /// <returns>A list of applications.</returns>
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

    /// <summary>
    /// Updates the status of a specific application (e.g., Shortlisted, Rejected).
    /// Only companies can update application status.
    /// </summary>
    /// <param name="id">The ID of the application.</param>
    /// <param name="request">The new status value.</param>
    /// <returns>A success message or 404.</returns>
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

    /// <summary>
    /// Submits a new application for an internship.
    /// Authenticated student identity is taken from the JWT token.
    /// </summary>
    /// <param name="request">The internship ID to apply for.</param>
    /// <returns>A success message.</returns>
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

    /// <summary>
    /// Retrieves all applications submitted by the authenticated student.
    /// </summary>
    /// <returns>A list of applications.</returns>
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

    /// <summary>
    /// Retrieves Key Performance Indicators (KPI) for the authenticated student's applications.
    /// </summary>
    /// <returns>KPI metrics.</returns>
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
