using Internova.Api.Presentation.Extensions;
using Internova.Api.Application.DTOs;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(IProjectRepository projectRepository) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProjects([FromQuery] string? category) =>
        Ok(await projectRepository.GetProjectsAsync(category));

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto)
    {
        var leaderId = User.GetUserId();

        var project = new Project
        {
            LeaderId = leaderId,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            RequiredSkills = dto.RequiredSkills,
            TeamSize = dto.TeamSize,
            Status = "Active",
            IsApproved = true
        };

        var created = await projectRepository.CreateProjectAsync(project);
        await projectRepository.AddProjectParticipationAsync(created.Id, leaderId, "Leader", "Accepted");

        return CreatedAtAction(nameof(GetProjects), new { id = created.Id }, created);
    }

    [HttpPost("{id:int}/join")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> RequestToJoin(int id)
    {
        var studentId = User.GetUserId();

        var project = await projectRepository.GetProjectByIdAsync(id);
        if (project is null)
            return NotFound(new { error = "Project not found" });

        if (project.LeaderId == studentId)
            return BadRequest(new { error = "You cannot join your own project." });

        var created = await projectRepository.AddProjectParticipationAsync(id, studentId, "Member", "Pending");
        return created
            ? Ok(new { message = "Request generated successfully." })
            : BadRequest(new { error = "Request already exists." });
    }

    [HttpGet("my-requests")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyRequests()
    {
        var studentId = User.GetUserId();
        return Ok(await projectRepository.GetStudentParticipationsAsync(studentId));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await projectRepository.GetProjectByIdAsync(id);
        if (project is null)
            return NotFound(new { error = "Project not found" });

        var deleted = await projectRepository.DeleteProjectAsync(id);
        return deleted
            ? Ok(new { message = "Project deleted successfully." })
            : StatusCode(500, new { error = "Failed to delete project" });
    }
}
