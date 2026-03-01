using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Internova.Infrastructure.Data;

/// <summary>
/// Runs once at application startup to:
/// 1. Verify the local MySQL server is reachable.
/// 2. Create the database if it does not exist (idempotent).
/// 3. Create the Users table if it does not already exist (idempotent).
/// </summary>
public static class DatabaseInitializer
{
    private const string CreateUsersTableSql = """
        CREATE TABLE IF NOT EXISTS Users (
            Id           INT            NOT NULL AUTO_INCREMENT,
            FullName     VARCHAR(200)   NOT NULL,
            Email        VARCHAR(320)   NOT NULL,
            PasswordHash VARCHAR(255)   NOT NULL,
            Role         VARCHAR(20)    NOT NULL,
            CreatedAt    DATETIME(6)    NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
            PRIMARY KEY (Id),
            UNIQUE INDEX UX_Users_Email (Email),
            CONSTRAINT CK_Users_Role CHECK (Role IN ('Student','Company','Admin'))
        );
        """;

    public static async Task InitializeAsync(IConfiguration configuration, ILogger logger)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogError(
                "ConnectionStrings:DefaultConnection is not configured. " +
                "Add it to appsettings.Development.json:\n\n" +
                "  \"ConnectionStrings\": {{\n" +
                "    \"DefaultConnection\": \"Server=localhost;Port=3306;Database=internova_db;User=root;Password=YOUR_PASSWORD;\"\n" +
                "  }}");

            throw new InvalidOperationException(
                "Database connection string is missing. See log output for setup instructions.");
        }

        // ── Step 1: Connect without a target database and create it if missing ──
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var targetDatabase = builder.Database;
        builder.Database = string.Empty; // connect to server root

        try
        {
            await using var rootConnection = new MySqlConnection(builder.ConnectionString);
            await rootConnection.OpenAsync();
            logger.LogInformation("✅ Connected to MySQL server at {Host}", builder.Server);

            var createDbSql = $"CREATE DATABASE IF NOT EXISTS `{targetDatabase}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
            await using var createDbCmd = new MySqlCommand(createDbSql, rootConnection);
            await createDbCmd.ExecuteNonQueryAsync();
            logger.LogInformation("✅ Database '{Database}' verified / created.", targetDatabase);
        }
        catch (MySqlException ex)
        {
            logger.LogError(ex,
                "❌ MySQL error {Number}: Could not connect to MySQL server. " +
                "Ensure MySQL is running locally and your credentials in appsettings.Development.json are correct.",
                ex.Number);
            throw new InvalidOperationException($"Failed to connect to MySQL server: {ex.Message}", ex);
        }

        // ── Step 2: Connect to the target database and create tables if missing ──
        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            await using var cmd = new MySqlCommand(CreateUsersTableSql, connection);
            await cmd.ExecuteNonQueryAsync();
            logger.LogInformation("✅ Users table verified / created.");
        }
        catch (MySqlException ex)
        {
            logger.LogError(ex,
                "❌ MySQL error {Number} when initializing tables in '{Database}'.",
                ex.Number, targetDatabase);
            throw new InvalidOperationException($"MySQL error {ex.Number}: {ex.Message}", ex);
        }
    }
}
