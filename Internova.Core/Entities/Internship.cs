using System.ComponentModel.DataAnnotations;

namespace Internova.Core.Entities;

/// <summary>
/// Represents an internship posting created by a Company.
/// </summary>
public class Internship
{
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Requirements { get; set; }

    public string? Duration { get; set; }

    public string? Location { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Active";

    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
