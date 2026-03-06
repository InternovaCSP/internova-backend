namespace Internova.Core.DTOs;

public class InternshipDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal? Stipend { get; set; }
    public string Skills { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
