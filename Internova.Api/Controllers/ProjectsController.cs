using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Internova.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(IProjectRepository projectRepository, ILogger<ProjectsController> logger) : ControllerBase
{
    // GET /api/projects: Allow query params ?category=Research
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProjects([FromQuery] string? category)
    {
        try
        {
            var projects = await projectRepository.GetProjectsAsync(category);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching projects");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // POST /api/projects: Create new project (Auto-assign creator as Leader)
    [HttpPost]
    [Authorize] // Student or any authorized user wanting to lead a project
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto)
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var creatorId))
        {
            return Unauthorized(new { error = "User identity could not be determined." });
        }

        try
        {
            var project = new Project
            {
                CreatorId = creatorId,
                Title = dto.Title,
                Description = dto.Description,
                Category = dto.Category,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            var createdProject = await projectRepository.CreateProjectAsync(project);

            // Auto-assign creator as Leader
            await projectRepository.AddProjectMemberAsync(createdProject.Id, creatorId, "Leader");

            return CreatedAtAction(nameof(GetProjects), new { id = createdProject.Id }, createdProject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating project");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // POST /api/projects/{id}/join: Student requests to join.
    [HttpPost("{id:int}/join")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> RequestToJoin(int id)
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new { error = "User identity could not be determined." });
        }

        try
        {
            var project = await projectRepository.GetProjectByIdAsync(id);
            if (project == null) return NotFound(new { error = "Project not found" });

            if (project.CreatorId == studentId)
            {
                return BadRequest(new { error = "You cannot join your own project." });
            }

            var success = await projectRepository.CreateJoinRequestAsync(id, studentId);
            if (!success)
            {
                // Most likely already requested
                return BadRequest(new { error = "Request already exists." });
            }

            return Ok(new { message = "Request generated successfully." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating join request");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // GET /api/projects/my-requests: Student sees their status.
    [HttpGet("my-requests")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyRequests()
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new { error = "User identity could not be determined." });
        }

        try
        {
            var requests = await projectRepository.GetStudentRequestsAsync(studentId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching requests");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
