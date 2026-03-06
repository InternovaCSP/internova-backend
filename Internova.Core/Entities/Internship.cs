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

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of internship (e.g., Full-time, Part-time, Remote).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal? Stipend { get; set; }

    /// <summary>
    /// Skills required for the internship (comma-separated or JSON string).
    /// </summary>
    public string Skills { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
