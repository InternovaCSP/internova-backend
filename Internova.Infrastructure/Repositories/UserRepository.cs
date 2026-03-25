using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace Internova.Infrastructure.Repositories;

/// <summary>Raw ADO.NET implementation of IUserRepository.</summary>
public class UserRepository(DbConnectionFactory connectionFactory) : IUserRepository
{
    private readonly DbConnectionFactory _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT TOP 1 user_id, full_name, email, password_hash, role, created_at
            FROM dbo.[User]
            WHERE email = @Email;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Email", email.ToLowerInvariant());

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapUser(reader);
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(int id)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT TOP 1 user_id, full_name, email, password_hash, role, created_at
            FROM dbo.[User]
            WHERE user_id = @Id;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapUser(reader);
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(User user)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            INSERT INTO dbo.[User] (full_name, email, password_hash, role, created_at)
            VALUES (@FullName, @Email, @PasswordHash, @Role, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@FullName", user.FullName);
        cmd.Parameters.AddWithValue("@Email", user.Email.ToLowerInvariant());
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@Role", user.Role);
        cmd.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);

        user.Id = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        return user.Id;
    }

    private static User MapUser(SqlDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("user_id")),
        FullName = r.GetString(r.GetOrdinal("full_name")),
        Email = r.GetString(r.GetOrdinal("email")),
        PasswordHash = r.GetString(r.GetOrdinal("password_hash")),
        Role = r.GetString(r.GetOrdinal("role")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("created_at"))
    };
}
