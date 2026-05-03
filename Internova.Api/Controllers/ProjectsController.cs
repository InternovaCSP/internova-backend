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

    // GET /api/projects/{id}: Get single project details
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProjectById(int id)
    {
        try
        {
            var project = await projectRepository.GetProjectByIdAsync(id);
            if (project == null) return NotFound(new { error = "Project not found" });
            return Ok(project);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching project {ProjectId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // POST /api/projects: Create new project (Auto-assign creator as Leader)
    [HttpPost]
    [Authorize] // Student or any authorized user wanting to lead a project
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto)
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var leaderId))
        {
            return Unauthorized(new { error = "User identity could not be determined." });
        }

        try
        {
            var project = new Project
            {
                LeaderId = leaderId,
                Title = dto.Title,
                Description = dto.Description,
                Category = dto.Category,
                RequiredSkills = dto.RequiredSkills,
                TeamSize = dto.TeamSize,
                Status = "Active",
                IsApproved = true // Auto approve for simplicity or set to false if admin approval required
            };

            var createdProject = await projectRepository.CreateProjectAsync(project);

            // Auto-assign creator as Leader ONLY if they are a Student (per User request)
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole == "Student")
            {
                await projectRepository.AddProjectParticipationAsync(createdProject.Id, leaderId, "Leader", "Accepted");
            }

            return CreatedAtAction(nameof(GetProjectById), new { id = createdProject.Id }, createdProject);
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

            if (project.LeaderId == studentId)
            {
                return BadRequest(new { error = "You cannot join your own project." });
            }

            var success = await projectRepository.AddProjectParticipationAsync(id, studentId, "Member", "Pending");
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
            var requests = await projectRepository.GetStudentParticipationsAsync(studentId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching requests");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // DELETE /api/projects/{id}: Admin deletes a project.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        try
        {
            var project = await projectRepository.GetProjectByIdAsync(id);
            if (project == null) return NotFound(new { error = "Project not found" });

            var deleted = await projectRepository.DeleteProjectAsync(id);
            if (!deleted) return StatusCode(500, new { error = "Failed to delete project" });

            return Ok(new { message = "Project deleted successfully." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting project {ProjectId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
