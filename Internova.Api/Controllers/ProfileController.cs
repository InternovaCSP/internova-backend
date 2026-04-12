using System.Security.Claims;
using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController(
    IUserRepository userRepository,
    IStudentProfileRepository studentProfileRepository,
    IBlobStorageService blobStorageService,
    ILogger<ProfileController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserProfileResponseDto>> GetProfile()
    {
        var userIdStr = User.FindFirstValue("user_id");
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var user = await userRepository.GetByIdAsync(userId);
        if (user == null) return NotFound();

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
            if (studentProfile != null)
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
    public async Task<IActionResult> UpdateProfile([FromForm] ProfileUpdateDto dto)
    {
        var userIdStr = User.FindFirstValue("user_id");
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var user = await userRepository.GetByIdAsync(userId);
        if (user == null) return NotFound();

        // 1. Update text fields
        user.FullName = dto.FullName;
        user.Bio = dto.Bio;
        user.Location = dto.Location;

        // 2. Handle Image Upload if provided
        if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)
        {
            try
            {
                await using var stream = dto.ProfilePicture.OpenReadStream();
                var imageUrl = await blobStorageService.UploadImageAsync(
                    stream,
                    dto.ProfilePicture.FileName,
                    dto.ProfilePicture.ContentType,
                    dto.ProfilePicture.Length,
                    userId);
                
                user.ProfilePictureUrl = imageUrl;
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to upload profile picture for user {UserId}", userId);
                // Continue without updating image if it fails, or return error?
                // For profile, image is usually optional, but if they tried to upload, we should probably tell them.
                return StatusCode(500, new { error = "Failed to upload image." });
            }
        }

        await userRepository.UpdateAsync(user);

        return Ok(new { 
            message = "Profile updated successfully", 
            profilePictureUrl = user.ProfilePictureUrl 
        });
    }
}
