using Internova.Api.Presentation.Extensions;
using Internova.Api.Application.DTOs;
using Internova.Api.Domain.Entities;
using Internova.Api.Domain.Enums;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController(
    IInternshipApplicationRepository applicationRepository,
    IStudentProfileRepository studentProfileRepository) : ControllerBase
{
    [HttpGet("student/{studentId:int}/profile")]
    [Authorize(Roles = "Company,Admin")]
    public async Task<IActionResult> GetStudentProfile(int studentId)
    {
        var profile = await studentProfileRepository.GetByUserIdAsync(studentId);
        return profile is null
            ? NotFound(new { error = "Student profile not found." })
            : Ok(profile);
    }

    [HttpGet("pipeline-stats")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetPipelineStats()
    {
        var studentId = User.GetUserId();
        return Ok(await applicationRepository.GetPipelineStatsAsync(studentId));
    }

    [HttpGet("company")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> GetCompanyApplications()
    {
        var companyId = User.GetUserId();
        return Ok(await applicationRepository.GetByCompanyIdAsync(companyId));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateApplicationStatusRequest request)
    {
        if (!Enum.TryParse<ApplicationStatus>(request.Status, out var status))
            return BadRequest(new { error = "Invalid status." });

        var updated = await applicationRepository.UpdateStatusAsync(id, status);
        return updated
            ? Ok(new { message = "Status updated successfully." })
            : NotFound(new { error = "Application not found." });
    }

    [HttpPost("apply")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Apply([FromBody] ApplyInternshipRequest request)
    {
        var studentId = User.GetUserId();

        var existing = await applicationRepository.GetByStudentIdAsync(studentId);
        if (existing.Any(a => a.InternshipId == request.InternshipId))
            return BadRequest(new { error = "You have already applied for this internship." });

        var now = DateTime.UtcNow;
        var application = new InternshipApplication
        {
            InternshipId = request.InternshipId,
            StudentId = studentId,
            Status = ApplicationStatus.Applied,
            AppliedAt = now,
            UpdatedAt = now
        };

        await applicationRepository.AddAsync(application);
        return Ok(new { message = "Application submitted successfully!" });
    }

    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentApplications()
    {
        var studentId = User.GetUserId();
        return Ok(await applicationRepository.GetByStudentIdAsync(studentId));
    }

    [HttpGet("kpi-stats")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetKpiStats()
    {
        var studentId = User.GetUserId();
        return Ok(await applicationRepository.GetKpiStatsAsync(studentId));
    }
}
