using System;

namespace Internova.Core.Entities;

public class Interview
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public DateTime InterviewDate { get; set; }
    public string LocationOrLink { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Optional navigation properties for convenience in UI/API
    public string? InternshipTitle { get; set; }
    public string? CompanyName { get; set; }
    public string? StudentName { get; set; }
}
