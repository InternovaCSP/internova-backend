using System.ComponentModel.DataAnnotations;

namespace Internova.Api.Application.DTOs;

public class CreateBreakoutRoomRequest
{
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;

    public string? AwardSkills { get; set; }
}
