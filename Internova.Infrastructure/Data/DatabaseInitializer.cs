using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Internova.Infrastructure.Data;

/// <summary>
/// Runs once at application startup to:
/// 1. Verify the Azure SQL database is reachable.
/// 2. Create dbo.Users if it does not already exist (idempotent).
/// </summary>
public static class DatabaseInitializer
{
    private const string CreateUsersTableSql = """
        IF OBJECT_ID('dbo.Users','U') IS NULL
        BEGIN
            CREATE TABLE dbo.Users (
                Id           INT            IDENTITY(1,1) NOT NULL,
                FullName     NVARCHAR(200)  NOT NULL,
                Email        NVARCHAR(320)  NOT NULL,
                PasswordHash NVARCHAR(255)  NOT NULL,
                Role         NVARCHAR(20)   NOT NULL,
                CreatedAt    DATETIME2      NOT NULL
                    CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
                CONSTRAINT PK_Users PRIMARY KEY (Id),
                CONSTRAINT CK_Users_Role CHECK (Role IN ('Student','Company','Admin'))
            );
            CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);
        END;
        """;

    public static async Task InitializeAsync(IConfiguration configuration, ILogger logger)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString.Contains("{your_password", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError(
                "ConnectionStrings:DefaultConnection is not configured. " +
                "Run the following commands to configure user-secrets:\n\n" +
                "  dotnet user-secrets init\n" +
                "  dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" " +
                "\"Server=tcp:internovacsp.database.windows.net,1433;Initial Catalog=internova_db;" +
                "Persist Security Info=False;User ID=internova_CS;Password=YOUR_PASSWORD;" +
                "MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;" +
                "Connection Timeout=30;\"\n" +
                "  dotnet user-secrets set \"Jwt:Key\" \"<generate-32+-char-random-secret>\"\n" +
                "  dotnet user-secrets set \"Jwt:Issuer\" \"Internova\"\n" +
                "  dotnet user-secrets set \"Jwt:Audience\" \"InternovaUsers\"");

            throw new InvalidOperationException(
                "Database connection string is missing or unconfigured. " +
                "See the log output above for setup instructions.");
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            logger.LogInformation("✅ Connected to Azure SQL: {Database}", connection.Database);

            await using var cmd = new SqlCommand(CreateUsersTableSql, connection);
            await cmd.ExecuteNonQueryAsync();
            logger.LogInformation("✅ dbo.Users table verified / created.");
        }
        catch (SqlException ex) when (ex.Number == 40613 ||
                                       ex.Message.Contains("not currently available", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError(ex,
                "❌ Azure SQL database 'internova_db' is PAUSED or temporarily unavailable (error 40613). " +
                "Go to Azure Portal → SQL databases → internova_db → Resume, then restart the app.");
            throw new InvalidOperationException(
                "Azure SQL database is paused. Resume it in the Azure Portal and try again.", ex);
        }
        catch (SqlException ex) when (ex.Message.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase) ||
                                       ex.Message.Contains("Login failed", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError(ex,
                "❌ Database 'internova_db' does not exist or credentials are incorrect. " +
                "Create the database in the Azure Portal before starting the application.");
            throw new InvalidOperationException(
                "Database 'internova_db' is not accessible. Create it in the Azure Portal.", ex);
        }
        catch (SqlException ex)
        {
            logger.LogError(ex,
                "❌ SQL error {Number} when connecting to Azure SQL. Check your connection string and firewall rules.",
                ex.Number);
            throw new InvalidOperationException(
                $"SQL error {ex.Number}: {ex.Message}", ex);
        }
    }
}
