namespace Internova.Core.Entities;

public class Project
{
    public int Id { get; set; }
    public int CreatorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional properties filled using joins during reporting
    public string? CreatorName { get; set; }
}
