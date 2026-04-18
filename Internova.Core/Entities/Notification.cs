namespace Internova.Core.Entities;

/// <summary>
/// Represents a notification for a user (Security, Social, System, etc.).
/// </summary>
public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? TargetUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
