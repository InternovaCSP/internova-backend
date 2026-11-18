using System.ComponentModel.DataAnnotations;

namespace Internova.Api.Application.DTOs;

public class CreateProjectDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? RequiredSkills { get; set; }

    public int? TeamSize { get; set; }
}

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

public class ProjectRequestResponseDto
{
    public int ParticipationId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}
