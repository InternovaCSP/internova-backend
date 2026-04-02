namespace Internova.Core.Entities;

public class Project
{
    public int Id { get; set; }
    public int LeaderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? RequiredSkills { get; set; }
    public int? TeamSize { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsApproved { get; set; }

    // Optional properties filled using joins during reporting
    public string? LeaderName { get; set; }
}
