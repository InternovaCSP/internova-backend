using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompetitionsController : ControllerBase
{
    private readonly ICompetitionRepository _competitionRepository;
    private readonly ILogger<CompetitionsController> _logger;

    public CompetitionsController(ICompetitionRepository competitionRepository, ILogger<CompetitionsController> logger)
    {
        _competitionRepository = competitionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all competitions. Accessible to Public/Students.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CompetitionDto>>> GetAll()
    {
        try
        {
            var competitions = await _competitionRepository.GetAllAsync();
            var dtos = competitions.Select(c => new CompetitionDto
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
                IsApproved = c.IsApproved
            });

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching competitions.");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new competition. Restricted to Admins.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CompetitionDto>> Create([FromBody] CreateCompetitionDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var competition = new Competition
            {
                OrganizerId = createDto.OrganizerId,
                Title = createDto.Title,
                Description = createDto.Description,
                Category = createDto.Category,
                EligibilityCriteria = createDto.EligibilityCriteria,
                StartDate = createDto.StartDate,
                EndDate = createDto.EndDate,
                RegistrationLink = createDto.RegistrationLink,
                IsApproved = false // Default to unapproved
            };

            var added = await _competitionRepository.AddAsync(competition);

            var resultDto = new CompetitionDto
            {
                Id = added.Id,
                OrganizerId = added.OrganizerId,
                Title = added.Title,
                Description = added.Description,
                Category = added.Category,
                EligibilityCriteria = added.EligibilityCriteria,
                StartDate = added.StartDate,
                EndDate = added.EndDate,
                RegistrationLink = added.RegistrationLink,
                IsApproved = added.IsApproved
            };

            return CreatedAtAction(nameof(GetAll), new { id = resultDto.Id }, resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating competition.");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates an existing competition. Restricted to Admins.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCompetitionDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var existing = await _competitionRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Competition with ID {id} not found.");
            }

            existing.Title = updateDto.Title;
            existing.Description = updateDto.Description;
            existing.Category = updateDto.Category;
            existing.EligibilityCriteria = updateDto.EligibilityCriteria;
            existing.StartDate = updateDto.StartDate;
            existing.EndDate = updateDto.EndDate;
            existing.RegistrationLink = updateDto.RegistrationLink;
            existing.IsApproved = updateDto.IsApproved;

            var success = await _competitionRepository.UpdateAsync(existing);
            if (!success)
            {
                return StatusCode(500, "Failed to update competition.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating competition {Id}.", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a competition. Restricted to Admins.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var existing = await _competitionRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Competition with ID {id} not found.");
            }

            var success = await _competitionRepository.DeleteAsync(id);
            if (!success)
            {
                return StatusCode(500, "Failed to delete competition.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting competition {Id}.", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
