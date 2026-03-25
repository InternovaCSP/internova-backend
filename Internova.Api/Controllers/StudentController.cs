using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Internova.Api.Controllers;

/// <summary>
/// Endpoints for authenticated Student users to manage their profile.
/// </summary>
[ApiController]
[Route("api/student")]
[Authorize(Roles = "Student")]
public class StudentController(
    IBlobStorageService blobStorageService,
    IStudentProfileRepository profileRepository,
    ILogger<StudentController> logger) : ControllerBase
{
    // ─── PUT /api/student/profile ─────────────────────────────────────────────

    /// <summary>
    /// Creates or updates the authenticated student's profile and uploads their resume PDF.
    /// </summary>
    [HttpPut("profile")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpsertProfile(
        [FromForm] string universityId,
        [FromForm] string? department,
        [FromForm] decimal gpa,
        [FromForm] string? skills,
        IFormFile? resume)
    {
        // ── Extract user_id from JWT ──────────────────────────────────────────

        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("PUT /api/student/profile: user_id claim missing or invalid.");
            return Unauthorized(new { error = "User identity could not be determined from token." });
        }

        // ── Validate inputs ───────────────────────────────────────────────────

        if (string.IsNullOrWhiteSpace(universityId))
            return BadRequest(new { error = "UniversityId is required." });

        if (gpa < 0 || gpa > 4.0m)
            return BadRequest(new { error = "GPA must be between 0.00 and 4.00." });

        if (resume is null || resume.Length == 0)
            return BadRequest(new { error = "A resume file is required." });

        // ── Upload to Azure Blob (extract stream/metadata from IFormFile here) ─

        string resumeUrl;
        try
        {
            await using var stream = resume.OpenReadStream();
            resumeUrl = await blobStorageService.UploadResumeAsync(
                stream,
                resume.FileName,
                resume.ContentType,
                resume.Length,
                userId);
        }
        catch (ArgumentException ex)
        {
            // File validation failure (wrong type, too large, etc.)
            logger.LogWarning(ex, "Resume upload validation failed for UserId {UserId}.", userId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Azure Blob upload failed for UserId {UserId}.", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to upload resume. Please try again later." });
        }

        // ── Upsert profile in database ────────────────────────────────────────

        StudentProfile saved;
        try
        {
            var profile = new StudentProfile
            {
                UserId       = userId,
                UniversityId = universityId.Trim(),
                Department   = department?.Trim() ?? string.Empty,
                GPA          = gpa,
                Skills       = skills?.Trim() ?? string.Empty,
                ResumeUrl    = resumeUrl,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow
            };

            saved = await profileRepository.UpsertAsync(profile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database upsert failed for UserId {UserId}.", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to save profile. Please try again later." });
        }

        logger.LogInformation("Profile upserted for UserId {UserId}, ProfileId {ProfileId}.", userId, saved.Id);

        return Ok(new
        {
            message   = "Upload Successful",
            resumeUrl = saved.ResumeUrl,
            profile   = new
            {
                saved.Id,
                saved.UserId,
                saved.UniversityId,
                saved.Department,
                saved.GPA,
                saved.Skills,
                saved.ResumeUrl,
                saved.CreatedAt,
                saved.UpdatedAt
            }
        });
    }
}
