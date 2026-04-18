namespace Internova.Core.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationSummaryDto
{
    public IEnumerable<NotificationDto> Items { get; set; } = [];
    public int UnreadCount { get; set; }
    public int TotalCount { get; set; }
}
