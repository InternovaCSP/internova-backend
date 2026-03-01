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

    /// <summary>Register a new Student or Company account.</summary>
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

    /// <summary>Authenticate with email and password; returns a JWT.</summary>
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

    // ─── GET /api/auth/me ─────────────────────────────────────────────────────

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

    // ─── JWT Generation ───────────────────────────────────────────────────────

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
