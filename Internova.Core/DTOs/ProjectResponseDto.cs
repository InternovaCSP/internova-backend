namespace Internova.Core.DTOs;

public class ProjectResponseDto
{
    public int Id { get; set; }
    public int CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
