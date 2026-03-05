using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Internova.Api.Controllers;

[ApiController]
[Route("api/internships")]
public class InternshipsController(
    IInternshipRepository internshipRepository,
    ILogger<InternshipsController> logger) : ControllerBase
{
    // ─── GET /api/internships ────────────────────────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var internships = await internshipRepository.GetAllAsync();
        return Ok(internships);
    }

    // ─── GET /api/internships/{id} ───────────────────────────────────────────
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var internship = await internshipRepository.GetByIdAsync(id);
        if (internship == null) return NotFound();
        return Ok(internship);
    }

    // ─── POST /api/internships ───────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> Create([FromBody] CreateInternshipDto dto)
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var companyId))
        {
            return Unauthorized(new { error = "Company identity could not be determined." });
        }

        var internship = new Internship
        {
            CompanyId = companyId,
            Title = dto.Title,
            Description = dto.Description,
            Type = dto.Type,
            Location = dto.Location,
            Stipend = dto.Stipend,
            Skills = dto.Skills,
            CreatedAt = DateTime.UtcNow
        };

        var created = await internshipRepository.AddAsync(internship);
        logger.LogInformation("Company {CompanyId} created internship {InternshipId}.", companyId, created.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // ─── PUT /api/internships/{id} ────────────────────────────────────────────
    [HttpPut("{id}")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateInternshipDto dto)
    {
        var existing = await internshipRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var companyId))
        {
            return Unauthorized();
        }

        if (existing.CompanyId != companyId)
        {
            return Forbid();
        }

        existing.Title = dto.Title;
        existing.Description = dto.Description;
        existing.Type = dto.Type;
        existing.Location = dto.Location;
        existing.Stipend = dto.Stipend;
        existing.Skills = dto.Skills;

        var success = await internshipRepository.UpdateAsync(existing);
        if (!success) return StatusCode(500, "Failed to update internship.");

        return Ok(existing);
    }

    // ─── DELETE /api/internships/{id} ─────────────────────────────────────────
    [HttpDelete("{id}")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await internshipRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var companyId))
        {
            return Unauthorized();
        }

        if (existing.CompanyId != companyId)
        {
            return Forbid();
        }

        var success = await internshipRepository.DeleteAsync(id);
        if (!success) return StatusCode(500, "Failed to delete internship.");

        return NoContent();
    }
}
