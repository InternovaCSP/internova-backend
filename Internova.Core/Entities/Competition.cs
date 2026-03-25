using System.ComponentModel.DataAnnotations;

namespace Internova.Core.Entities;

/// <summary>
/// Represents a competition or hackathon.
/// </summary>
public class Competition
{
    public int Id { get; set; }

    [Required]
    public int OrganizerId { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    public string? EligibilityCriteria { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(2048)]
    public string? RegistrationLink { get; set; }

    public bool IsApproved { get; set; } = false;
    public string? OrganizerName { get; set; }
}
