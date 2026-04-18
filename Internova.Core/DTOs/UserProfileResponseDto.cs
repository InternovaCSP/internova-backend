namespace Internova.Core.DTOs;

public class UserProfileResponseDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? ProfilePictureUrl { get; set; }
    
    // Academic fields (optional, only for students)
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
