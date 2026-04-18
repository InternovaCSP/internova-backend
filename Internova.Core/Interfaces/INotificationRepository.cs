using Internova.Core.Entities;

namespace Internova.Core.Interfaces;

public interface INotificationRepository
{
    /// <summary>Retrieves paginated notifications for a user, sorted by most recent.</summary>
    Task<(IEnumerable<Notification> Items, int TotalCount, int UnreadCount)> GetByUserIdAsync(int userId, int page = 1, int pageSize = 10);

    /// <summary>Marks a specific notification as read.</summary>
    Task MarkAsReadAsync(int notificationId);

    /// <summary>Marks all notifications as read for a specific user.</summary>
    Task MarkAllAsReadAsync(int userId);

    /// <summary>Persists a new notification.</summary>
    Task<int> CreateAsync(Notification notification);
}
