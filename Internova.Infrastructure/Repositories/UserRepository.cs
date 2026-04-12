using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace Internova.Infrastructure.Repositories;

public class UserRepository(DbConnectionFactory connectionFactory) : IUserRepository
{
    private readonly DbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<User?> GetByEmailAsync(string email)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT TOP 1 user_id, full_name, email, password_hash, role,
                   email_notifications_enabled, push_notifications_enabled, theme_preference,
                   bio, location, profile_picture_url, created_at
            FROM dbo.[User]
            WHERE email = @Email;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Email", email.ToLowerInvariant());

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapUser(reader);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT TOP 1 user_id, full_name, email, password_hash, role,
                   email_notifications_enabled, push_notifications_enabled, theme_preference,
                   bio, location, profile_picture_url, created_at
            FROM dbo.[User]
            WHERE user_id = @Id;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapUser(reader);
    }

    public async Task<int> CreateAsync(User user)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            INSERT INTO dbo.[User] (full_name, email, password_hash, role, 
                                   email_notifications_enabled, push_notifications_enabled, 
                                   theme_preference, created_at)
            VALUES (@FullName, @Email, @PasswordHash, @Role, 
                    @EmailNotif, @PushNotif, @ThemePref, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@FullName", user.FullName);
        cmd.Parameters.AddWithValue("@Email", user.Email.ToLowerInvariant());
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@Role", user.Role);
        cmd.Parameters.AddWithValue("@EmailNotif", user.EmailNotificationsEnabled);
        cmd.Parameters.AddWithValue("@PushNotif", user.PushNotificationsEnabled);
        cmd.Parameters.AddWithValue("@ThemePref", user.ThemePreference);
        cmd.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);

        user.Id = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        return user.Id;
    }

    // ✅ SETTINGS UPDATE
    public async Task UpdateSettingsAsync(int userId, bool emailNotif, bool pushNotif, string theme)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            UPDATE dbo.[User]
            SET email_notifications_enabled = @EmailNotif,
                push_notifications_enabled = @PushNotif,
                theme_preference = @ThemePref
            WHERE user_id = @Id;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@EmailNotif", emailNotif);
        cmd.Parameters.AddWithValue("@PushNotif", pushNotif);
        cmd.Parameters.AddWithValue("@ThemePref", theme);
        cmd.Parameters.AddWithValue("@Id", userId);

        await cmd.ExecuteNonQueryAsync();
    }

    // ✅ PROFILE UPDATE
    public async Task UpdateAsync(User user)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            UPDATE dbo.[User]
            SET full_name = @FullName,
                bio = @Bio,
                location = @Location,
                profile_picture_url = @ProfilePictureUrl
            WHERE user_id = @Id;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@FullName", user.FullName);
        cmd.Parameters.AddWithValue("@Bio", (object?)user.Bio ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Location", (object?)user.Location ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)user.ProfilePictureUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Id", user.Id);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            UPDATE dbo.[User]
            SET password_hash = @PasswordHash
            WHERE user_id = @Id;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@PasswordHash", newPasswordHash);
        cmd.Parameters.AddWithValue("@Id", userId);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int userId)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var transaction = connection.BeginTransaction();
        try
        {
            const string sqlProfiles = "DELETE FROM dbo.StudentProfile WHERE user_id = @Id;";
            await using (var cmdProfile = new SqlCommand(sqlProfiles, connection, transaction))
            {
                cmdProfile.Parameters.AddWithValue("@Id", userId);
                await cmdProfile.ExecuteNonQueryAsync();
            }

            const string sqlUser = "DELETE FROM dbo.[User] WHERE user_id = @Id;";
            await using (var cmdUser = new SqlCommand(sqlUser, connection, transaction))
            {
                cmdUser.Parameters.AddWithValue("@Id", userId);
                await cmdUser.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static User MapUser(SqlDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("user_id")),
        FullName = r.GetString(r.GetOrdinal("full_name")),
        Email = r.GetString(r.GetOrdinal("email")),
        PasswordHash = r.GetString(r.GetOrdinal("password_hash")),
        Role = r.GetString(r.GetOrdinal("role")),

        EmailNotificationsEnabled = r.GetBoolean(r.GetOrdinal("email_notifications_enabled")),
        PushNotificationsEnabled = r.GetBoolean(r.GetOrdinal("push_notifications_enabled")),
        ThemePreference = r.GetString(r.GetOrdinal("theme_preference")),

        Bio = r.IsDBNull(r.GetOrdinal("bio")) ? null : r.GetString(r.GetOrdinal("bio")),
        Location = r.IsDBNull(r.GetOrdinal("location")) ? null : r.GetString(r.GetOrdinal("location")),
        ProfilePictureUrl = r.IsDBNull(r.GetOrdinal("profile_picture_url")) ? null : r.GetString(r.GetOrdinal("profile_picture_url")),

        CreatedAt = r.GetDateTime(r.GetOrdinal("created_at"))
    };
}