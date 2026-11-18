using Internova.Api.Presentation.Extensions;
using Internova.Api.Presentation.Requests;
using Internova.Api.Application.DTOs;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfilesController(
    IUserRepository userRepository,
    IStudentProfileRepository studentProfileRepository,
    IBlobStorageService blobStorageService,
    ILogger<ProfilesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserProfileResponseDto>> GetProfile()
    {
        var userId = User.GetUserId();
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return NotFound();

        var response = new UserProfileResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Bio = user.Bio,
            Location = user.Location,
            ProfilePictureUrl = user.ProfilePictureUrl
        };

        if (user.Role == "Student")
        {
            var studentProfile = await studentProfileRepository.GetByUserIdAsync(userId);
            if (studentProfile is not null)
            {
                response.AcademicProfile = new StudentProfileDto
                {
                    UniversityId = studentProfile.UniversityId,
                    Department = studentProfile.Department,
                    GPA = studentProfile.GPA,
                    Skills = studentProfile.Skills,
                    ResumeUrl = studentProfile.ResumeUrl
                };
            }
        }

        return Ok(response);
    }

    [HttpPut]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateProfile([FromForm] ProfileUpdateRequest dto)
    {
        var userId = User.GetUserId();
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return NotFound();

        user.FullName = dto.FullName;
        user.Bio = dto.Bio;
        user.Location = dto.Location;

        if (dto.ProfilePicture is { Length: > 0 })
        {
            try
            {
                await using var stream = dto.ProfilePicture.OpenReadStream();
                user.ProfilePictureUrl = await blobStorageService.UploadImageAsync(
                    stream,
                    dto.ProfilePicture.FileName,
                    dto.ProfilePicture.ContentType,
                    dto.ProfilePicture.Length,
                    userId);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to upload profile picture for user {UserId}", userId);
                return StatusCode(500, new { error = "Failed to upload image." });
            }
        }

        await userRepository.UpdateAsync(user);

        return Ok(new
        {
            message = "Profile updated successfully",
            profilePictureUrl = user.ProfilePictureUrl
        });
    }
}
