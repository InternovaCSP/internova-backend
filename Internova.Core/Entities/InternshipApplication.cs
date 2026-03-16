using Internova.Core.Enums;

namespace Internova.Core.Entities;

public class InternshipApplication
{
    public int Id { get; set; }
    public int InternshipId { get; set; }
    public int StudentId { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties (not used for ADO.NET but good for clarity)
    public string? InternshipTitle { get; set; }
    public string? CompanyName { get; set; }
    public string? StudentName { get; set; }
}
