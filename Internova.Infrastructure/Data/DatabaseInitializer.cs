using Internova.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Internova.Infrastructure.Data;

/// <summary>
/// Runs once at application startup to:
/// 1. Verify the local SQL Server is reachable.
/// 2. Create the database if it does not exist (idempotent).
/// 3. Create the Users table if it does not already exist (idempotent).
/// 4. Seed a default Admin user if none exists.
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

            // ── Create User Table if missing (Safety check) ──
            const string createUserTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'User')
                BEGIN
                    CREATE TABLE [User] (
                        user_id INT IDENTITY(1,1) PRIMARY KEY,
                        full_name VARCHAR(255) NOT NULL,
                        email VARCHAR(255) UNIQUE NOT NULL,
                        password_hash VARCHAR(MAX) NOT NULL,
                        role VARCHAR(50) CHECK (role IN ('Student', 'Company', 'Admin', 'Faculty', 'Organizer')),
                        is_approved BIT DEFAULT 0,
                        created_at DATETIME2 DEFAULT GETDATE(),
                        updated_at DATETIME2 DEFAULT GETDATE()
                    );
                END";
            await using var createUserCmd = new SqlCommand(createUserTableSql, connection);
            await createUserCmd.ExecuteNonQueryAsync();

            // ── Create Company_Profile Table if missing ──
            const string createCompanyProfileTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Company_Profile')
                BEGIN
                    CREATE TABLE Company_Profile (
                        company_id INT PRIMARY KEY,
                        company_name VARCHAR(255) NOT NULL,
                        industry VARCHAR(100),
                        address TEXT,
                        description TEXT,
                        website_url VARCHAR(2048),
                        is_verified BIT DEFAULT 0,
                        status VARCHAR(50) DEFAULT 'Pending',
                        CONSTRAINT FK_Company_User FOREIGN KEY (company_id) REFERENCES [User](user_id) ON DELETE CASCADE
                    );
                END";
            await using var createCompanyProfileCmd = new SqlCommand(createCompanyProfileTableSql, connection);
            await createCompanyProfileCmd.ExecuteNonQueryAsync();
            logger.LogInformation("✅ Company_Profile table verified / created.");

            // ── Create Internship Table if missing ──
            const string createInternshipTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Internship')
                BEGIN
                    CREATE TABLE Internship (
                        internship_id INT IDENTITY(1,1) PRIMARY KEY,
                        company_id INT NOT NULL,
                        title VARCHAR(255) NOT NULL,
                        description TEXT,
                        requirements TEXT,
                        duration VARCHAR(100),
                        location VARCHAR(255),
                        status VARCHAR(50) DEFAULT 'Active' CHECK (status IN ('Active', 'Closed')),
                        is_published BIT DEFAULT 0,
                        created_at DATETIME2 DEFAULT GETDATE(),
                        company_description NVARCHAR(MAX),
                        CONSTRAINT FK_Internship_Company FOREIGN KEY (company_id) REFERENCES Company_Profile(company_id)
                    );
                END";
            await using var createInternshipCmd = new SqlCommand(createInternshipTableSql, connection);
            await createInternshipCmd.ExecuteNonQueryAsync();
            logger.LogInformation("✅ Internship table verified / created.");

            // ── Create Competition Table if missing ──
            const string createCompetitionTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Competition')
                BEGIN
                    CREATE TABLE Competition (
                        competition_id INT IDENTITY(1,1) PRIMARY KEY,
                        organizer_id INT NOT NULL,
                        title VARCHAR(255) NOT NULL,
                        description TEXT,
                        category VARCHAR(100),
                        eligibility_criteria TEXT,
                        start_date DATE,
                        end_date DATE,
                        registration_link VARCHAR(2048),
                        is_approved BIT DEFAULT 0,
                        CONSTRAINT FK_Competition_Organizer FOREIGN KEY (organizer_id) REFERENCES [User](user_id)
                    );
                END";
            await using var createCompetitionCmd = new SqlCommand(createCompetitionTableSql, connection);
            await createCompetitionCmd.ExecuteNonQueryAsync();
            logger.LogInformation("✅ Competition table verified / created.");

            // ── Ensure 'company_description' exists in Internship (Migration) ──
            const string migrateInternshipTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Internship') AND name = 'company_description')
                BEGIN
                    ALTER TABLE Internship ADD company_description NVARCHAR(MAX);
                END";
            await using var migrateInternshipCmd = new SqlCommand(migrateInternshipTableSql, connection);
            await migrateInternshipCmd.ExecuteNonQueryAsync();
            logger.LogInformation("✅ Internship schema migration verified.");

            // ── Create Internship_Application Table if missing ──
            const string createApplicationTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Internship_Application')
                BEGIN
                    CREATE TABLE Internship_Application (
                        application_id INT IDENTITY(1,1) PRIMARY KEY,
                        internship_id INT NOT NULL,
                        student_id INT NOT NULL,
                        status VARCHAR(50) DEFAULT 'Applied' CHECK (status IN ('Applied', 'Shortlisted', 'Interviewing', 'Selected', 'Rejected')),
                        applied_at DATETIME2 DEFAULT GETDATE(),
                        updated_at DATETIME2 DEFAULT GETDATE(),
                        CONSTRAINT FK_Application_Internship FOREIGN KEY (internship_id) REFERENCES Internship(internship_id),
                        CONSTRAINT FK_Application_Student FOREIGN KEY (student_id) REFERENCES [User](user_id)
                    );
                END";
            await using var createApplicationCmd = new SqlCommand(createApplicationTableSql, connection);
            await createApplicationCmd.ExecuteNonQueryAsync();
            logger.LogInformation("✅ Internship_Application table verified / created.");

            // ── Ensure 'updated_at' exists (Migration) ──
            const string migrateApplicationTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Internship_Application') AND name = 'updated_at')
                BEGIN
                    ALTER TABLE Internship_Application ADD updated_at DATETIME2 DEFAULT GETDATE();
                END";
            await using var migrateApplicationCmd = new SqlCommand(migrateApplicationTableSql, connection);
            await migrateApplicationCmd.ExecuteNonQueryAsync();
            logger.LogInformation("✅ Internship_Application schema migration verified.");

            // ── Step 3: Seed Admin User ──
            await SeedAdminUserAsync(connection, logger);
        }
        catch (SqlException ex)
        {
            logger.LogError(ex,
                "❌ SQL Server error {Number} when connecting to '{Database}'.",
                ex.Number, targetDatabase);
            throw new InvalidOperationException($"SQL Server error {ex.Number}: {ex.Message}", ex);
        }
    }

    private static async Task SeedAdminUserAsync(SqlConnection connection, ILogger logger)
    {
        const string checkAdminSql = "SELECT COUNT(*) FROM [User] WHERE email = @Email";
        await using var checkCmd = new SqlCommand(checkAdminSql, connection);
        checkCmd.Parameters.AddWithValue("@Email", "admin@internova.com");
        var adminExists = (int)await checkCmd.ExecuteScalarAsync() > 0;

        if (!adminExists)
        {
            logger.LogInformation("🚀 Seeding default Admin user...");

            var adminUser = new User
            {
                FullName = "System Admin",
                Email = "admin@internova.com",
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            var hasher = new PasswordHasher<User>();
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@123");

            const string insertAdminSql = @"
                INSERT INTO [User] (full_name, email, password_hash, role, is_approved, created_at, updated_at)
                VALUES (@FullName, @Email, @PasswordHash, @Role, 1, @CreatedAt, @CreatedAt)";

            await using var insertCmd = new SqlCommand(insertAdminSql, connection);
            insertCmd.Parameters.AddWithValue("@FullName", adminUser.FullName);
            insertCmd.Parameters.AddWithValue("@Email", adminUser.Email);
            insertCmd.Parameters.AddWithValue("@PasswordHash", adminUser.PasswordHash);
            insertCmd.Parameters.AddWithValue("@Role", adminUser.Role);
            insertCmd.Parameters.AddWithValue("@CreatedAt", adminUser.CreatedAt);

            await insertCmd.ExecuteNonQueryAsync();
            logger.LogInformation("✅ Default Admin user created: admin@internova.com / Admin@123");
        }
        else
        {
            logger.LogInformation("ℹ️ Default Admin user 'admin@internova.com' already exists.");
        }
    }
}


