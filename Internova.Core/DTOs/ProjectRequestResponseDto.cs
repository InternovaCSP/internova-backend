namespace Internova.Core.DTOs;

public class ProjectRequestResponseDto
{
    public int RequestId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}
