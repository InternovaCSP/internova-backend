using System.Reflection;
using System.Text.RegularExpressions;
using Internova.Api.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace Internova.Api.Infrastructure.Data;

/// <summary>
/// Runs once at startup to ensure the database, schema (from embedded scripts/schema.sql),
/// and default Admin user exist.
/// </summary>
public static partial class DatabaseInitializer
{
    private const string SchemaResourceName = "Internova.Api.schema.sql";

    public static async Task InitializeAsync(IConfiguration configuration, ILogger logger)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        var targetDatabase = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using (var masterConnection = new SqlConnection(builder.ConnectionString))
        {
            await masterConnection.OpenAsync();
            var checkDbSql = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = @DbName) CREATE DATABASE [{targetDatabase}];";
            await using var cmd = new SqlCommand(checkDbSql, masterConnection);
            cmd.Parameters.AddWithValue("@DbName", targetDatabase);
            await cmd.ExecuteNonQueryAsync();
            logger.LogInformation("Database '{Database}' verified.", targetDatabase);
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await ExecuteEmbeddedSchemaAsync(connection);
        logger.LogInformation("Database schema verified.");

        await SeedAdminUserAsync(connection, logger);
    }

    private static async Task ExecuteEmbeddedSchemaAsync(SqlConnection connection)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(SchemaResourceName)
            ?? throw new InvalidOperationException($"Embedded schema resource '{SchemaResourceName}' was not found.");

        using var reader = new StreamReader(stream);
        var schemaSql = await reader.ReadToEndAsync();

        var batches = GoBatchRegex().Split(schemaSql)
            .Select(b => b.Trim())
            .Where(b => !string.IsNullOrWhiteSpace(b));

        foreach (var batch in batches)
        {
            await using var cmd = new SqlCommand(batch, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task SeedAdminUserAsync(SqlConnection connection, ILogger logger)
    {
        const string checkSql = "SELECT COUNT(*) FROM dbo.[User] WHERE email = @Email";
        await using var checkCmd = new SqlCommand(checkSql, connection);
        checkCmd.Parameters.AddWithValue("@Email", "admin@internova.com");

        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync() ?? 0) > 0)
            return;

        var admin = new User
        {
            FullName = "System Admin",
            Email = "admin@internova.com",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, "Admin@123");

        const string insertSql = """
            INSERT INTO dbo.[User] (full_name, email, password_hash, role, is_approved, created_at, updated_at)
            VALUES (@FullName, @Email, @PasswordHash, @Role, 1, @CreatedAt, @CreatedAt);
            """;

        await using var insertCmd = new SqlCommand(insertSql, connection);
        insertCmd.Parameters.AddWithValue("@FullName", admin.FullName);
        insertCmd.Parameters.AddWithValue("@Email", admin.Email);
        insertCmd.Parameters.AddWithValue("@PasswordHash", admin.PasswordHash);
        insertCmd.Parameters.AddWithValue("@Role", admin.Role);
        insertCmd.Parameters.AddWithValue("@CreatedAt", admin.CreatedAt);
        await insertCmd.ExecuteNonQueryAsync();

        logger.LogInformation("Default Admin user seeded: admin@internova.com");
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoBatchRegex();
}
