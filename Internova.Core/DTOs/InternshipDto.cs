namespace Internova.Core.DTOs;

public class InternshipDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? Duration { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}
