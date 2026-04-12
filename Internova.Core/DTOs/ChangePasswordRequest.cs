using System.ComponentModel.DataAnnotations;

namespace Internova.Core.DTOs;

/// <summary>
/// Domain Transfer Object for changing a user's password.
/// </summary>
public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters long.")]
    public string NewPassword { get; set; } = string.Empty;
}
