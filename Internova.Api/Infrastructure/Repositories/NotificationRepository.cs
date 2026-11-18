using Dapper;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;

namespace Internova.Api.Infrastructure.Repositories;

public class NotificationRepository(DbConnectionFactory connectionFactory) : INotificationRepository
{
    public async Task<(IEnumerable<Notification> Items, int TotalCount, int UnreadCount)> GetByUserIdAsync(
        int userId,
        int page = 1,
        int pageSize = 10)
    {
        const string sql = """
            SELECT notification_id AS Id,
                   user_id AS UserId,
                   type AS Type,
                   content AS Content,
                   target_url AS TargetUrl,
                   is_read AS IsRead,
                   created_at AS CreatedAt
            FROM dbo.Notification
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM dbo.Notification WHERE user_id = @UserId;
            SELECT COUNT(*) FROM dbo.Notification WHERE user_id = @UserId AND is_read = 0;
            """;

        await using var connection = connectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, new
        {
            UserId = userId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        var items = (await multi.ReadAsync<Notification>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();
        var unreadCount = await multi.ReadSingleAsync<int>();

        return (items, totalCount, unreadCount);
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        const string sql = "UPDATE dbo.Notification SET is_read = 1 WHERE notification_id = @Id;";

        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = notificationId });
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        const string sql = "UPDATE dbo.Notification SET is_read = 1 WHERE user_id = @UserId AND is_read = 0;";

        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId });
    }

    public async Task<int> CreateAsync(Notification notification)
    {
        const string sql = """
            INSERT INTO dbo.Notification (user_id, type, content, target_url, is_read, created_at)
            VALUES (@UserId, @Type, @Content, @TargetUrl, @IsRead, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = connectionFactory.CreateConnection();
        notification.Id = await connection.ExecuteScalarAsync<int>(sql, notification);
        return notification.Id;
    }
}
