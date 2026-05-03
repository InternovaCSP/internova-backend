using System.Security.Claims;
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
            var userIdClaim = User.FindFirstValue("user_id");
            int? currentStudentId = null;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                currentStudentId = id;
            }

            var competitions = await _competitionRepository.GetAllAsync();
            var dtos = new List<CompetitionDto>();

            foreach (var c in competitions)
            {
                string? status = null;
                if (currentStudentId.HasValue && User.IsInRole("Student"))
                {
                    var isRegistered = await _competitionRepository.IsStudentRegisteredAsync(c.Id, currentStudentId.Value);
                    status = isRegistered ? "Registered" : null;
                }

                dtos.Add(new CompetitionDto
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
                    OrganizerName = c.OrganizerName,
                    Skills = c.Skills,
                    CurrentUserStatus = status
                });
            }

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
            var userIdClaim = User.FindFirstValue("user_id");
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var adminId))
            {
                return Unauthorized(new { error = "Admin identity could not be determined." });
            }

            var competition = new Competition
            {
                OrganizerId = adminId,
                Title = createDto.Title,
                Description = createDto.Description,
                Category = createDto.Category,
                EligibilityCriteria = createDto.EligibilityCriteria,
                StartDate = createDto.StartDate,
                EndDate = createDto.EndDate,
                RegistrationLink = createDto.RegistrationLink,
                Skills = createDto.Skills,
                IsApproved = true // Default to approved if admin creates it directly
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
                IsApproved = added.IsApproved,
                Skills = added.Skills
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
            existing.Skills = updateDto.Skills;

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

    /// <summary>
    /// Registers a student for a competition.
    /// </summary>
    [HttpPost("{id}/register")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Register(int id)
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new { error = "Student identity could not be determined." });
        }

        try
        {
            var competition = await _competitionRepository.GetByIdAsync(id);
            if (competition == null)
            {
                return NotFound(new { error = "Competition not found." });
            }

            var success = await _competitionRepository.RegisterStudentAsync(id, studentId);
            if (!success)
            {
                return BadRequest(new { error = "Registration failed or already registered." });
            }

            return Ok(new { message = "Successfully registered for the competition!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering student {StudentId} for competition {Id}", studentId, id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
