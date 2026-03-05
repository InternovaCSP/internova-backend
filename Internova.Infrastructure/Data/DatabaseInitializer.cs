using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Internova.Infrastructure.Data;

/// <summary>
/// Runs once at application startup to:
/// 1. Verify the local SQL Server is reachable.
/// 2. Create the database if it does not exist (idempotent).
/// 3. Create the Users table if it does not already exist (idempotent).
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IConfiguration configuration, ILogger logger)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogError(
                "ConnectionStrings:DefaultConnection is not configured. " +
                "Add it to appsettings.Development.json.");

            throw new InvalidOperationException(
                "Database connection string is missing. See log output for setup instructions.");
        }

        // ── Step 1: Connect without a target database and create it if missing ──
        var builder = new SqlConnectionStringBuilder(connectionString);
        var targetDatabase = builder.InitialCatalog;
        builder.InitialCatalog = "master"; // connect to master database

        try
        {
            await using var masterConnection = new SqlConnection(builder.ConnectionString);
            await masterConnection.OpenAsync();
            logger.LogInformation("✅ Connected to SQL Server master at {DataSource}", builder.DataSource);

            var checkDbSql = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{targetDatabase}') CREATE DATABASE [{targetDatabase}];";
            await using var checkDbCmd = new SqlCommand(checkDbSql, masterConnection);
            await checkDbCmd.ExecuteNonQueryAsync();
            logger.LogInformation("✅ Database '{Database}' verified / created.", targetDatabase);
        }
        catch (SqlException ex)
        {
            logger.LogError(ex,
                "❌ SQL Server error {Number}: Could not connect to SQL Server. " +
                "Ensure LocalDB is installed and running.",
                ex.Number);
            throw new InvalidOperationException($"Failed to connect to SQL Server: {ex.Message}", ex);
        }

        // ── Step 2: Verify connectivity to the target database ──
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            logger.LogInformation("✅ Successfully connected to database '{Database}'.", targetDatabase);
        }
        catch (SqlException ex)
        {
            logger.LogError(ex,
                "❌ SQL Server error {Number} when connecting to '{Database}'.",
                ex.Number, targetDatabase);
            throw new InvalidOperationException($"SQL Server error {ex.Number}: {ex.Message}", ex);
        }
    }
}
