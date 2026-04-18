using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace Internova.Infrastructure.Repositories;

public class NotificationRepository(DbConnectionFactory connectionFactory) : INotificationRepository
{
    private readonly DbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<(IEnumerable<Notification> Items, int TotalCount, int UnreadCount)> GetByUserIdAsync(int userId, int page = 1, int pageSize = 10)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        // Get notifications (paginated)
        const string sqlItems = """
            SELECT notification_id, user_id, type, content, target_url, is_read, created_at
            FROM Notification
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        // Get total count and unread count
        const string sqlCounts = """
            SELECT COUNT(*) FROM Notification WHERE user_id = @UserId;
            SELECT COUNT(*) FROM Notification WHERE user_id = @UserId AND is_read = 0;
            """;

        var items = new List<Notification>();
        int totalCount = 0;
        int unreadCount = 0;

        await using (var cmdItems = new SqlCommand(sqlItems, connection))
        {
            cmdItems.Parameters.AddWithValue("@UserId", userId);
            cmdItems.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            cmdItems.Parameters.AddWithValue("@PageSize", pageSize);

            await using var reader = await cmdItems.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapNotification(reader));
            }
        }

        await using (var cmdCounts = new SqlCommand(sqlCounts, connection))
        {
            cmdCounts.Parameters.AddWithValue("@UserId", userId);
            await using var reader = await cmdCounts.ExecuteReaderAsync();
            
            if (await reader.ReadAsync()) totalCount = reader.GetInt32(0);
            if (await reader.NextResultAsync() && await reader.ReadAsync()) unreadCount = reader.GetInt32(0);
        }

        return (items, totalCount, unreadCount);
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "UPDATE Notification SET is_read = 1 WHERE notification_id = @Id;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", notificationId);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "UPDATE Notification SET is_read = 1 WHERE user_id = @UserId AND is_read = 0;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@UserId", userId);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> CreateAsync(Notification notification)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            INSERT INTO Notification (user_id, type, content, target_url, is_read, created_at)
            VALUES (@UserId, @Type, @Content, @TargetUrl, @IsRead, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@UserId", notification.UserId);
        cmd.Parameters.AddWithValue("@Type", notification.Type);
        cmd.Parameters.AddWithValue("@Content", notification.Content);
        cmd.Parameters.AddWithValue("@TargetUrl", (object?)notification.TargetUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsRead", notification.IsRead);
        cmd.Parameters.AddWithValue("@CreatedAt", notification.CreatedAt);

        var id = (int)await cmd.ExecuteScalarAsync();
        notification.Id = id;
        return id;
    }

    private static Notification MapNotification(SqlDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("notification_id")),
        UserId = r.GetInt32(r.GetOrdinal("user_id")),
        Type = r.GetString(r.GetOrdinal("type")),
        Content = r.GetString(r.GetOrdinal("content")),
        TargetUrl = r.IsDBNull(r.GetOrdinal("target_url")) ? null : r.GetString(r.GetOrdinal("target_url")),
        IsRead = r.GetBoolean(r.GetOrdinal("is_read")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("created_at"))
    };
}
