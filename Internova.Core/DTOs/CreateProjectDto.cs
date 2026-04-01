using System.ComponentModel.DataAnnotations;

namespace Internova.Core.DTOs;

public class CreateProjectDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
}
