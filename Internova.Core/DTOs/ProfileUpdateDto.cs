using Microsoft.AspNetCore.Http;

namespace Internova.Core.DTOs;

/// <summary>
/// Data transfer object for updating a user's personal profile.
/// Supports multipart/form-data for image uploads.
/// </summary>
public class ProfileUpdateDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public IFormFile? ProfilePicture { get; set; }
}
