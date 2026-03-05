using System.ComponentModel.DataAnnotations;

namespace Internova.Core.DTOs;

public class CreateInternshipDto
{
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal? Stipend { get; set; }

    public string Skills { get; set; } = string.Empty;
}
