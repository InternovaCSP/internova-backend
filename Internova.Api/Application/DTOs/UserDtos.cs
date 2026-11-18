namespace Internova.Api.Application.DTOs;

public class UserProfileResponseDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public StudentProfileDto? AcademicProfile { get; set; }
}

public class StudentProfileDto
{
    public string UniversityId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal GPA { get; set; }
    public string Skills { get; set; } = string.Empty;
    public string ResumeUrl { get; set; } = string.Empty;
}

public record UpdateSettingsRequest(
    bool EmailNotifications,
    bool PushNotifications,
    string ThemePreference);
