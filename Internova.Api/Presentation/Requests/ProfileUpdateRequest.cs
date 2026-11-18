namespace Internova.Api.Presentation.Requests;

/// <summary>
/// Multipart form payload for updating a user's personal profile.
/// </summary>
public class ProfileUpdateRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public IFormFile? ProfilePicture { get; set; }
}
