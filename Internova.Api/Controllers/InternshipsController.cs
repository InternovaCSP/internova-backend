using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Internova.Api.Controllers;

/// <summary>
/// Manages internship postings. 
/// Companies can create/update/delete their own postings.
/// Students and Guests can browse active postings.
/// </summary>
[ApiController]
[Route("api/internships")]
public class InternshipsController(
    IInternshipRepository internshipRepository,
    ICompanyProfileRepository companyRepository,
    IUserRepository userRepository,
    ILogger<InternshipsController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves all internship postings created by the currently authenticated company.
    /// </summary>
    /// <returns>A list of internship entities.</returns>
    [HttpGet("my/postings")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> GetForCompany()
    {
        try
        {
            logger.LogInformation(">>> GetForCompany (my/postings) CALLED <<<");
            var userIdClaim = User.FindFirstValue("user_id");
            logger.LogInformation("Claim 'user_id' = {UserId}", userIdClaim ?? "NULL");

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var companyId))
            {
                logger.LogWarning("Blocking request: Invalid/Missing user_id claim");
                return Unauthorized(new { error = "Company identity could not be determined." });
            }

            logger.LogInformation("Fetching from repo for Company ID: {CompanyId}", companyId);
            var internships = await internshipRepository.GetByCompanyIdAsync(companyId);
            var result = internships.ToList();
            logger.LogInformation("Successfully found {Count} internships", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FATAL: GetForCompany failure");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all active and published internship postings.
    /// Accessible to everyone.
    /// </summary>
    /// <returns>A list of active internships.</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var internships = await internshipRepository.GetAllAsync();
        var approvedAndPublished = internships
            .Where(i => i.Status == "Active" && i.IsPublished)
            .ToList();
        return Ok(approvedAndPublished);
    }

    /// <summary>
    /// Retrieves a specific internship posting by its unique ID.
    /// </summary>
    /// <param name="id">The ID of the internship.</param>
    /// <returns>The internship entity or 404 Not Found.</returns>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var internship = await internshipRepository.GetByIdAsync(id);
        if (internship == null) return NotFound();
        return Ok(internship);
    }

    /// <summary>
    /// Creates a new internship posting for the authenticated company.
    /// If the company profile is missing, a stub profile is created.
    /// </summary>
    /// <param name="dto">The internship details.</param>
    /// <returns>The created internship with a link to its location.</returns>
    [HttpPost]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> Create([FromBody] CreateInternshipDto dto)
    {
        var userIdClaim = User.FindFirstValue("user_id");

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var companyId))
        {
            return Unauthorized(new { error = "Company identity could not be determined." });
        }

        // Check if company profile exists
        var profile = await companyRepository.GetByCompanyIdAsync(companyId);

        if (profile == null)
        {
            logger.LogInformation("CompanyProfile missing for user {UserId}. Creating stub.", companyId);
            var user = await userRepository.GetByIdAsync(companyId);
            profile = new CompanyProfile
            {
                CompanyId = companyId,
                CompanyName = user?.FullName ?? "Unknown Company",
                Status = Internova.Core.Enums.CompanyStatus.Pending,
                IsVerified = false
            };
            await companyRepository.AddAsync(profile);
        }

        var initialStatus = (profile.Status == Internova.Core.Enums.CompanyStatus.Active) 
            ? "Active" 
            : "Pending Approval";

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

    /// <summary>
    /// Updates an existing internship posting.
    /// Only the company that created the posting can update it.
    /// </summary>
    /// <param name="id">The ID of the internship to update.</param>
    /// <param name="dto">The updated internship details.</param>
    /// <returns>The updated internship entity.</returns>
    [HttpPut("{id:int}")]
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
        existing.Duration = dto.Duration;
        existing.Location = dto.Location;
        existing.Requirements = dto.Requirements;
        existing.IsPublished = dto.IsPublished;

        var success = await internshipRepository.UpdateAsync(existing);
        if (!success) return StatusCode(500, "Failed to update internship.");

        return Ok(existing);
    }

    /// <summary>
    /// Deletes an internship posting.
    /// Only the company that created the posting can delete it.
    /// </summary>
    /// <param name="id">The ID of the internship to delete.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpDelete("{id:int}")]
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
