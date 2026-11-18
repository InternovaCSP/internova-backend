using Internova.Api.Presentation.Extensions;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

/// <summary>
/// Endpoints for authenticated Student users to manage their academic profile and resume.
/// </summary>
[ApiController]
[Route("api/student")]
[Authorize(Roles = "Student")]
public class StudentsController(
    IBlobStorageService blobStorageService,
    IStudentProfileRepository profileRepository,
    ILogger<StudentsController> logger) : ControllerBase
{
    private const string DefaultResumeFallbackUrl =
        "https://resumesector.com/wp-content/uploads/2024/10/Best-Resume-Template-Free-Download-MS-Word--724x1024.jpg";

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
        var userId = User.GetUserId();

        if (string.IsNullOrWhiteSpace(universityId))
            return BadRequest(new { error = "UniversityId is required." });

        if (gpa is < 0 or > 4.0m)
            return BadRequest(new { error = "GPA must be between 0.00 and 4.00." });

        var resumeUrl = await UploadResumeOrFallbackAsync(resume, userId);

        var now = DateTime.UtcNow;
        var profile = new StudentProfile
        {
            UserId = userId,
            UniversityId = universityId.Trim(),
            Department = department?.Trim() ?? string.Empty,
            GPA = gpa,
            Skills = skills?.Trim() ?? string.Empty,
            ResumeUrl = resumeUrl,
            CreatedAt = now,
            UpdatedAt = now
        };

        var saved = await profileRepository.UpsertAsync(profile);
        logger.LogInformation("Profile upserted for UserId {UserId}, ProfileId {ProfileId}.", userId, saved.Id);

        return Ok(new
        {
            message = "Upload Successful",
            resumeUrl = saved.ResumeUrl,
            profile = new
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

    private async Task<string> UploadResumeOrFallbackAsync(IFormFile? resume, int userId)
    {
        if (resume is not { Length: > 0 })
        {
            logger.LogInformation("No resume file provided for UserId {UserId}. Using fallback placeholder.", userId);
            return DefaultResumeFallbackUrl;
        }

        try
        {
            await using var stream = resume.OpenReadStream();
            return await blobStorageService.UploadResumeAsync(
                stream,
                resume.FileName,
                resume.ContentType,
                resume.Length,
                userId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Azure Blob upload failed for UserId {UserId}. Using fallback placeholder.", userId);
            return DefaultResumeFallbackUrl;
        }
    }
}
