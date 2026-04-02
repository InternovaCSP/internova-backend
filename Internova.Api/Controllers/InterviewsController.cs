using Internova.Core.Entities;
using Internova.Core.Enums;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Internova.Api.Controllers;

[ApiController]
[Route("api/interviews")]
[Authorize]
public class InterviewsController(
    IInterviewRepository interviewRepository,
    IInternshipApplicationRepository applicationRepository,
    ILogger<InterviewsController> logger) : ControllerBase
{
    // ─── POST /api/interviews/schedule ─────────────────────────────────────
    [HttpPost("schedule")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> ScheduleInterview([FromBody] ScheduleInterviewRequest request)
    {
        try
        {
            // For now, we assume the company owns the application. 
            // In a real app, we'd verify the internship belongs to this company.
            
            var interview = new Interview
            {
                ApplicationId = request.ApplicationId,
                InterviewDate = request.InterviewDate,
                LocationOrLink = request.LocationOrLink,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await interviewRepository.AddAsync(interview);

            // Automatically move application status to InterviewScheduled
            await applicationRepository.UpdateStatusAsync(request.ApplicationId, ApplicationStatus.InterviewScheduled);

            return Ok(new { message = "Interview scheduled successfully.", interviewId = interview.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to schedule interview for application {ApplicationId}", request.ApplicationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ─── GET /api/interviews/student ───────────────────────────────────────
    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentInterviews()
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new { error = "Student identity could not be determined." });
        }

        try
        {
            var interviews = await interviewRepository.GetByStudentIdAsync(studentId);
            return Ok(interviews);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch interviews for student {StudentId}", studentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ─── GET /api/interviews/company ───────────────────────────────────────
    [HttpGet("company")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> GetCompanyInterviews()
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var companyId))
        {
            return Unauthorized(new { error = "Company identity could not be determined." });
        }

        try
        {
            var interviews = await interviewRepository.GetByCompanyIdAsync(companyId);
            return Ok(interviews);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch interviews for company {CompanyId}", companyId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    public class ScheduleInterviewRequest
    {
        public int ApplicationId { get; set; }
        public DateTime InterviewDate { get; set; }
        public string LocationOrLink { get; set; } = string.Empty;
    }
}
