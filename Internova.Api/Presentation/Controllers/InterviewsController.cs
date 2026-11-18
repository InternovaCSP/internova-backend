using Internova.Api.Presentation.Extensions;
using Internova.Api.Application.DTOs;
using Internova.Api.Domain.Entities;
using Internova.Api.Domain.Enums;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

[ApiController]
[Route("api/interviews")]
[Authorize]
public class InterviewsController(
    IInterviewRepository interviewRepository,
    IInternshipApplicationRepository applicationRepository) : ControllerBase
{
    [HttpPost("schedule")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> ScheduleInterview([FromBody] ScheduleInterviewRequest request)
    {
        var now = DateTime.UtcNow;
        var interview = new Interview
        {
            ApplicationId = request.ApplicationId,
            InterviewDate = request.InterviewDate,
            LocationOrLink = request.LocationOrLink,
            CreatedAt = now,
            UpdatedAt = now
        };

        await interviewRepository.AddAsync(interview);
        await applicationRepository.UpdateStatusAsync(request.ApplicationId, ApplicationStatus.InterviewScheduled);

        return Ok(new { message = "Interview scheduled successfully.", interviewId = interview.Id });
    }

    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentInterviews()
    {
        var studentId = User.GetUserId();
        return Ok(await interviewRepository.GetByStudentIdAsync(studentId));
    }

    [HttpGet("company")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> GetCompanyInterviews()
    {
        var companyId = User.GetUserId();
        return Ok(await interviewRepository.GetByCompanyIdAsync(companyId));
    }
}
