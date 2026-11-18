using Internova.Api.Presentation.Extensions;
using Internova.Api.Application.DTOs;
using Internova.Api.Domain.Entities;
using Internova.Api.Domain.Enums;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

[ApiController]
[Route("api/internships")]
public class InternshipsController(
    IInternshipRepository internshipRepository,
    ICompanyProfileRepository companyRepository,
    IUserRepository userRepository,
    ILogger<InternshipsController> logger) : ControllerBase
{
    [HttpGet("my/postings")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> GetForCompany()
    {
        if (!User.TryGetUserId(out var companyId))
            return Unauthorized(new { error = "Company identity could not be determined." });

        var internships = await internshipRepository.GetByCompanyIdAsync(companyId);
        return Ok(internships.ToList());
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var internships = await internshipRepository.GetAllAsync();
        var published = internships
            .Where(i => i.Status == "Active" && i.IsPublished)
            .ToList();
        return Ok(published);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var internship = await internshipRepository.GetByIdAsync(id);
        return internship is null ? NotFound() : Ok(internship);
    }

    [HttpPost]
    [Authorize(Roles = "Company", Policy = "RequireCompanyApproval")]
    public async Task<IActionResult> Create([FromBody] CreateInternshipDto dto)
    {
        if (!User.TryGetUserId(out var companyId))
            return Unauthorized(new { error = "Company identity could not be determined." });

        var profile = await companyRepository.GetByCompanyIdAsync(companyId);
        if (profile is null)
        {
            var user = await userRepository.GetByIdAsync(companyId);
            profile = new CompanyProfile
            {
                CompanyId = companyId,
                CompanyName = user?.FullName ?? "Unknown Company",
                Status = CompanyStatus.Pending,
                IsVerified = false
            };
            await companyRepository.AddAsync(profile);
        }

        var initialStatus = profile.Status == CompanyStatus.Active ? "Active" : "Pending Approval";
        var internship = new Internship
        {
            CompanyId = companyId,
            Title = dto.Title,
            Description = dto.Description,
            Duration = dto.Duration,
            Location = dto.Location,
            Requirements = dto.Requirements,
            IsPublished = dto.IsPublished,
            Status = initialStatus,
            CreatedAt = DateTime.UtcNow
        };

        var created = await internshipRepository.AddAsync(internship);
        logger.LogInformation("Company {CompanyId} created internship {InternshipId} with status {Status}.",
            companyId, created.Id, initialStatus);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateInternshipDto dto)
    {
        var existing = await internshipRepository.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        if (!User.TryGetUserId(out var companyId))
            return Unauthorized();

        if (existing.CompanyId != companyId)
            return Forbid();

        existing.Title = dto.Title;
        existing.Description = dto.Description;
        existing.Duration = dto.Duration;
        existing.Location = dto.Location;
        existing.Requirements = dto.Requirements;
        existing.IsPublished = dto.IsPublished;

        var updated = await internshipRepository.UpdateAsync(existing);
        return updated ? Ok(existing) : StatusCode(500, "Failed to update internship.");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await internshipRepository.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        if (!User.TryGetUserId(out var companyId))
            return Unauthorized();

        if (existing.CompanyId != companyId)
            return Forbid();

        var deleted = await internshipRepository.DeleteAsync(id);
        return deleted ? NoContent() : StatusCode(500, "Failed to delete internship.");
    }
}
