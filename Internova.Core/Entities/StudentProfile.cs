namespace Internova.Core.Entities;

/// <summary>
/// Represents a Student's academic and professional profile.
/// One profile per user (UserId is unique FK to Users.Id).
/// </summary>
public class StudentProfile
{
    /// <summary>Auto-increment PK.</summary>
    public int Id { get; set; }

    /// <summary>FK → Users.Id. One profile per student.</summary>
    public int UserId { get; set; }

    public string UniversityId { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    /// <summary>GPA on a 0–4.0 scale.</summary>
    public decimal GPA { get; set; }

    /// <summary>Comma-separated or free-form skills text.</summary>
    public string Skills { get; set; } = string.Empty;

    /// <summary>Full Azure Blob URL of the uploaded resume PDF.</summary>
    public string ResumeUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
