namespace Internova.Core.DTOs;

public class ProjectResponseDto
{
    public int Id { get; set; }
    public int LeaderId { get; set; }
    public string LeaderName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? RequiredSkills { get; set; }
    public int? TeamSize { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}
