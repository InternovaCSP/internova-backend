using System.ComponentModel.DataAnnotations;

namespace Internova.Core.DTOs;

/// <summary>Payload for POST /api/auth/register.</summary>
public class RegisterRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Allowed values: "Student", "Company". Admins cannot self-register.</summary>
    [Required]
    public string Role { get; set; } = string.Empty;
}
