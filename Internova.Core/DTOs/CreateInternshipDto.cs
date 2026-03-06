using System.ComponentModel.DataAnnotations;

namespace Internova.Core.DTOs;

public class CreateInternshipDto
{
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string? Description { get; set; }

    public string? Duration { get; set; }

    public string? Location { get; set; }

    public string? Requirements { get; set; }

    public bool IsPublished { get; set; } = false;
}
