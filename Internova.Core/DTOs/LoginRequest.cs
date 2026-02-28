using System.ComponentModel.DataAnnotations;

namespace Internova.Core.DTOs;

/// <summary>Payload for POST /api/auth/login.</summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
