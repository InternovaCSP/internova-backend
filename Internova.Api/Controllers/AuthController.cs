using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Internova.Api.Controllers;

/// <summary>
/// Authentication endpoints: Register, Login, Me.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(
    IUserRepository userRepository,
    IConfiguration configuration) : ControllerBase
{
    private static readonly HashSet<string> AllowedRegistrationRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Student", "Company" };

    // ─── POST /api/auth/register ──────────────────────────────────────────────

    /// <summary>
    /// Registers a new Student or Company account in the system.
    /// Validates uniqueness of the email and hashes the password securely before saving.
    /// </summary>
    /// <param name="request">The registration payload carrying FullName, Email, Password, and Role.</param>
    /// <returns>A 201 Created response containing the new user ID, or a 400/409 on error.</returns>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Enforce: Admin accounts cannot be self-registered
        if (!AllowedRegistrationRoles.Contains(request.Role))
            return BadRequest(new { error = "Role must be 'Student' or 'Company'. Admin accounts cannot be self-registered." });

        // Check for duplicate email
        var existing = await userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
            return Conflict(new { error = "An account with this email address already exists." });

        // Hash password using ASP.NET built-in PasswordHasher
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        var userId = await userRepository.CreateAsync(user);

        return StatusCode(StatusCodes.Status201Created, new { userId, message = "Account created successfully." });
    }

    // ─── POST /api/auth/login ─────────────────────────────────────────────────

    /// <summary>
    /// Authenticates a user using their email and password.
    /// Upon successful verification, generates and returns a cryptographically signed JWT.
    /// </summary>
    /// <param name="request">The login payload carrying Email and Password.</param>
    /// <returns>A 200 OK containing the AuthResponse (JWT token, UserId, Role, Email) or 401 Unauthorized.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Normalise email for lookup
        var user = await userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());

        // Generic error — do not reveal whether email exists
        if (user is null)
            return Unauthorized(new { error = "Invalid email or password." });

        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Invalid email or password." });

        var token = GenerateJwt(user);

        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role
        });
    }

    /// <summary>Returns the claims of the currently authenticated user.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(new { claims });
    }

    // ─── POST /api/auth/change-password ───────────────────────────────────────

    /// <summary>
    /// Updates the password for the current user. 
    /// Requires the current password for verification before applying the new one.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "User identity missing." });

        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound(new { error = "User not found." });

        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);

        if (result == PasswordVerificationResult.Failed)
            return BadRequest(new { error = "The current password provided is incorrect." });

        var newHash = hasher.HashPassword(user, request.NewPassword);
        await userRepository.UpdatePasswordAsync(userId, newHash);

        return Ok(new { message = "Password updated successfully." });
    }

    // ─── DELETE /api/auth/account ─────────────────────────────────────────────

    /// <summary>
    /// Permanently deletes the authenticated user's account and all associated data.
    /// This action is irreversible.
    /// </summary>
    [HttpDelete("account")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "User identity missing." });

        await userRepository.DeleteAsync(userId);

        return Ok(new { message = "Your account has been permanently deleted." });
    }

    // ─── JWT Generation ───────────────────────────────────────────────────────

    /// <summary>
    /// Generates a signed JSON Web Token (JWT) encapsulating the user's primary identity claims.
    /// Uses HMAC SHA-256 for signing, configured via the Jwt:Key setting.
    /// </summary>
    /// <param name="user">The fully authenticated User entity.</param>
    /// <returns>A base64 encoded JWT string.</returns>
    private string GenerateJwt(User user)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "Internova";
        var audience = configuration["Jwt:Audience"] ?? "InternovaUsers";

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("user_id", user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
