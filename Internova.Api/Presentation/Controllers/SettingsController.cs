using Internova.Api.Presentation.Extensions;
using Internova.Api.Application.DTOs;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

/// <summary>
/// Endpoints for managing user-specific settings and preferences.
/// </summary>
[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController(IUserRepository userRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSettings()
    {
        var userId = User.GetUserId();
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return NotFound(new { error = "User not found." });

        return Ok(new
        {
            emailNotifications = user.EmailNotificationsEnabled,
            pushNotifications = user.PushNotificationsEnabled,
            themePreference = user.ThemePreference
        });
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        var userId = User.GetUserId();

        await userRepository.UpdateSettingsAsync(
            userId,
            request.EmailNotifications,
            request.PushNotifications,
            request.ThemePreference);

        return Ok(new { message = "Settings updated successfully." });
    }
}
