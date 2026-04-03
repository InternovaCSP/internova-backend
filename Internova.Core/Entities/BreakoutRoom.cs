using System.ComponentModel.DataAnnotations;

namespace Internova.Core.Entities;

/// <summary>
/// Represents a student-led breakout room/meeting.
/// </summary>
public class BreakoutRoom
{
    public int Id { get; set; }

    /// <summary>FK → Users.Id (The student who created the room).</summary>
    [Required]
    public int OrganizerId { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }

    /// <summary>Generated Google Meet link.</summary>
    [StringLength(2048)]
    public string? MeetingLink { get; set; }

    /// <summary>Room status: Scheduled, Active, Completed, Cancelled.</summary>
    [StringLength(50)]
    public string Status { get; set; } = "Scheduled";

    /// <summary>Comma-separated list of skills awarded to participants.</summary>
    public string? AwardSkills { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Optional: Navigation property if EF is used for queries
    public string? OrganizerName { get; set; }
}
