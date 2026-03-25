namespace Internova.Core.DTOs;

public class CompetitionDto
{
    public int Id { get; set; }
    public int OrganizerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? EligibilityCriteria { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? RegistrationLink { get; set; }
    public bool IsApproved { get; set; }
    public string? OrganizerName { get; set; }
}

public class CreateCompetitionDto
{
    public int OrganizerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? EligibilityCriteria { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? RegistrationLink { get; set; }
}

public class UpdateCompetitionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? EligibilityCriteria { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? RegistrationLink { get; set; }
    public bool IsApproved { get; set; }
}
