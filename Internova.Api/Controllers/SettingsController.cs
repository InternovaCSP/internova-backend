using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Internova.Api.Controllers;

/// <summary>
/// Endpoints for managing user-specific settings and preferences.
/// </summary>
[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController(IUserRepository userRepository) : ControllerBase
{
    // ─── GET /api/settings ────────────────────────────────────────────────────
    
    /// <summary>
    /// Retrieves the current user's preferences (Theme, Notifications).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSettings()
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "User identity could not be determined." });

        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound(new { error = "User not found." });

        return Ok(new
        {
            emailNotifications = user.EmailNotificationsEnabled,
            pushNotifications = user.PushNotificationsEnabled,
            themePreference = user.ThemePreference
        });
    }

    // ─── PUT /api/settings ────────────────────────────────────────────────────

    /// <summary>
    /// Updates the current user's preferences.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "User identity could not be determined." });

        await userRepository.UpdateSettingsAsync(
            userId, 
            request.EmailNotifications, 
            request.PushNotifications, 
            request.ThemePreference);

        return Ok(new { message = "Settings updated successfully." });
    }
}

public record UpdateSettingsRequest(
    bool EmailNotifications,
    bool PushNotifications,
    string ThemePreference);
