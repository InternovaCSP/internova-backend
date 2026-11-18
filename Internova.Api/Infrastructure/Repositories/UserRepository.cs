using Dapper;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;

namespace Internova.Api.Infrastructure.Repositories;

public class UserRepository(DbConnectionFactory connectionFactory) : IUserRepository
{
    private const string SelectColumns = """
        user_id AS Id,
        full_name AS FullName,
        email AS Email,
        password_hash AS PasswordHash,
        role AS Role,
        email_notifications_enabled AS EmailNotificationsEnabled,
        push_notifications_enabled AS PushNotificationsEnabled,
        theme_preference AS ThemePreference,
        bio AS Bio,
        location AS Location,
        profile_picture_url AS ProfilePictureUrl,
        created_at AS CreatedAt
        """;

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = $"""
            SELECT TOP 1 {SelectColumns}
            FROM dbo.[User]
            WHERE email = @Email;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email.ToLowerInvariant() });
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        const string sql = $"""
            SELECT TOP 1 {SelectColumns}
            FROM dbo.[User]
            WHERE user_id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(User user)
    {
        const string sql = """
            INSERT INTO dbo.[User] (
                full_name, email, password_hash, role,
                email_notifications_enabled, push_notifications_enabled,
                theme_preference, created_at)
            VALUES (
                @FullName, @Email, @PasswordHash, @Role,
                @EmailNotificationsEnabled, @PushNotificationsEnabled,
                @ThemePreference, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = connectionFactory.CreateConnection();
        user.Id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            user.FullName,
            Email = user.Email.ToLowerInvariant(),
            user.PasswordHash,
            user.Role,
            user.EmailNotificationsEnabled,
            user.PushNotificationsEnabled,
            user.ThemePreference,
            user.CreatedAt
        });

        return user.Id;
    }

    public async Task UpdateSettingsAsync(int userId, bool emailNotif, bool pushNotif, string theme)
    {
        const string sql = """
            UPDATE dbo.[User]
            SET email_notifications_enabled = @EmailNotif,
                push_notifications_enabled = @PushNotif,
                theme_preference = @Theme
            WHERE user_id = @UserId;
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            EmailNotif = emailNotif,
            PushNotif = pushNotif,
            Theme = theme
        });
    }

    public async Task UpdateAsync(User user)
    {
        const string sql = """
            UPDATE dbo.[User]
            SET full_name = @FullName,
                bio = @Bio,
                location = @Location,
                profile_picture_url = @ProfilePictureUrl
            WHERE user_id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, user);
    }

    public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
    {
        const string sql = """
            UPDATE dbo.[User]
            SET password_hash = @PasswordHash
            WHERE user_id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = userId, PasswordHash = newPasswordHash });
    }

    public async Task DeleteAsync(int userId)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            await connection.ExecuteAsync(
                "DELETE FROM dbo.Student_Profile WHERE student_id = @Id;",
                new { Id = userId },
                transaction);

            await connection.ExecuteAsync(
                "DELETE FROM dbo.[User] WHERE user_id = @Id;",
                new { Id = userId },
                transaction);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
