using Internova.Api.Application.DTOs;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompetitionsController(ICompetitionRepository competitionRepository) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CompetitionDto>>> GetAll()
    {
        var competitions = await competitionRepository.GetAllAsync();
        return Ok(competitions.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CompetitionDto>> Create([FromBody] CreateCompetitionDto dto)
    {
        var competition = new Competition
        {
            OrganizerId = dto.OrganizerId,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            EligibilityCriteria = dto.EligibilityCriteria,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            RegistrationLink = dto.RegistrationLink,
            IsApproved = false
        };

        var added = await competitionRepository.AddAsync(competition);
        return CreatedAtAction(nameof(GetAll), new { id = added.Id }, ToDto(added));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCompetitionDto dto)
    {
        var existing = await competitionRepository.GetByIdAsync(id);
        if (existing is null)
            return NotFound($"Competition with ID {id} not found.");

        existing.Title = dto.Title;
        existing.Description = dto.Description;
        existing.Category = dto.Category;
        existing.EligibilityCriteria = dto.EligibilityCriteria;
        existing.StartDate = dto.StartDate;
        existing.EndDate = dto.EndDate;
        existing.RegistrationLink = dto.RegistrationLink;
        existing.IsApproved = dto.IsApproved;

        var updated = await competitionRepository.UpdateAsync(existing);
        return updated ? NoContent() : StatusCode(500, "Failed to update competition.");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await competitionRepository.GetByIdAsync(id);
        if (existing is null)
            return NotFound($"Competition with ID {id} not found.");

        var deleted = await competitionRepository.DeleteAsync(id);
        return deleted ? NoContent() : StatusCode(500, "Failed to delete competition.");
    }

    private static CompetitionDto ToDto(Competition c) => new()
    {
        Id = c.Id,
        OrganizerId = c.OrganizerId,
        Title = c.Title,
        Description = c.Description,
        Category = c.Category,
        EligibilityCriteria = c.EligibilityCriteria,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        RegistrationLink = c.RegistrationLink,
        IsApproved = c.IsApproved,
        OrganizerName = c.OrganizerName
    };
}
