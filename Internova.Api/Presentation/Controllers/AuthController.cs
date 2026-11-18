using Internova.Api.Presentation.Extensions;
using Internova.Api.Application.DTOs;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

/// <summary>
/// Authentication endpoints: Register, Login, Me, Password Management, and Account Deletion.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(
    IUserRepository userRepository,
    IAuthService authService) : ControllerBase
{
    private static readonly HashSet<string> AllowedRegistrationRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Student", "Company" };

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!AllowedRegistrationRoles.Contains(request.Role))
            return BadRequest(new { error = "Role must be 'Student' or 'Company'. Admin accounts cannot be self-registered." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await userRepository.GetByEmailAsync(email) is not null)
            return Conflict(new { error = "An account with this email address already exists." });

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = authService.HashPassword(user, request.Password);

        var userId = await userRepository.CreateAsync(user);
        return StatusCode(StatusCodes.Status201Created, new { userId, message = "Account created successfully." });
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || !authService.VerifyPassword(user, user.PasswordHash, request.Password))
            return Unauthorized(new { error = "Invalid email or password." });

        return Ok(new AuthResponse
        {
            Token = authService.GenerateJwtToken(user),
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role
        });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(new { claims });
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.GetUserId();
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return NotFound(new { error = "User not found." });

        if (!authService.VerifyPassword(user, user.PasswordHash, request.CurrentPassword))
            return BadRequest(new { error = "The current password provided is incorrect." });

        var newHash = authService.HashPassword(user, request.NewPassword);
        await userRepository.UpdatePasswordAsync(userId, newHash);

        return Ok(new { message = "Password updated successfully." });
    }

    [HttpDelete("account")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.GetUserId();
        await userRepository.DeleteAsync(userId);
        return Ok(new { message = "Your account has been permanently deleted." });
    }
}
