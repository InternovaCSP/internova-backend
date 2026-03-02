using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using MySqlConnector;
using Microsoft.Extensions.Logging;

namespace Internova.Infrastructure.Repositories;

/// <summary>
/// ADO.NET repository for StudentProfile using raw MySqlConnector queries.
/// Uses INSERT … ON DUPLICATE KEY UPDATE for a single-roundtrip upsert.
/// </summary>
public class StudentProfileRepository : IStudentProfileRepository
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<StudentProfileRepository> _logger;

    public StudentProfileRepository(DbConnectionFactory connectionFactory, ILogger<StudentProfileRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a single StudentProfile entity matching the provided unique User ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the User mapping to the profile.</param>
    /// <returns>The populated StudentProfile object if found, otherwise null.</returns>
    public async Task<StudentProfile?> GetByUserIdAsync(int userId)
    {
        await using var connection = (MySqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT Id, UserId, UniversityId, Department, GPA, Skills, ResumeUrl, CreatedAt, UpdatedAt
            FROM StudentProfiles
            WHERE UserId = @UserId
            LIMIT 1;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapProfile(reader);
    }

    /// <summary>
    /// Inserts a new StudentProfile or updates the existing one if it already exists for the User.
    /// Leverages an atomic MySQL INSERT ... ON DUPLICATE KEY UPDATE query to minimize round-trips.
    /// </summary>
    /// <param name="profile">The fully populated StudentProfile object to persist.</param>
    /// <returns>The freshly persisted profile as stored in the database.</returns>
    public async Task<StudentProfile> UpsertAsync(StudentProfile profile)
    {
        await using var connection = (MySqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var now = DateTime.UtcNow;
        profile.UpdatedAt = now;

        // Single-roundtrip upsert: INSERT ... ON DUPLICATE KEY UPDATE
        // UNIQUE INDEX on UserId triggers the UPDATE path when the profile already exists.
        const string sql = """
            INSERT INTO StudentProfiles
                (UserId, UniversityId, Department, GPA, Skills, ResumeUrl, CreatedAt, UpdatedAt)
            VALUES
                (@UserId, @UniversityId, @Department, @GPA, @Skills, @ResumeUrl, @CreatedAt, @UpdatedAt)
            ON DUPLICATE KEY UPDATE
                UniversityId = VALUES(UniversityId),
                Department   = VALUES(Department),
                GPA          = VALUES(GPA),
                Skills       = VALUES(Skills),
                ResumeUrl    = VALUES(ResumeUrl),
                UpdatedAt    = VALUES(UpdatedAt);

            SELECT Id, UserId, UniversityId, Department, GPA, Skills, ResumeUrl, CreatedAt, UpdatedAt
            FROM StudentProfiles
            WHERE UserId = @UserId
            LIMIT 1;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@UserId",       profile.UserId);
        cmd.Parameters.AddWithValue("@UniversityId", profile.UniversityId);
        cmd.Parameters.AddWithValue("@Department",   profile.Department);
        cmd.Parameters.AddWithValue("@GPA",          profile.GPA);
        cmd.Parameters.AddWithValue("@Skills",       profile.Skills);
        cmd.Parameters.AddWithValue("@ResumeUrl",    profile.ResumeUrl);
        cmd.Parameters.AddWithValue("@CreatedAt",    profile.CreatedAt == default ? now : profile.CreatedAt);
        cmd.Parameters.AddWithValue("@UpdatedAt",    now);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var saved = MapProfile(reader);

        _logger.LogInformation("Upserted StudentProfile for UserId {UserId} → ProfileId {ProfileId}.",
            profile.UserId, saved.Id);

        return saved;
    }

    private static StudentProfile MapProfile(MySqlDataReader r) => new()
    {
        Id           = r.GetInt32("Id"),
        UserId       = r.GetInt32("UserId"),
        UniversityId = r.GetString("UniversityId"),
        Department   = r.IsDBNull(r.GetOrdinal("Department")) ? string.Empty : r.GetString("Department"),
        GPA          = r.GetDecimal("GPA"),
        Skills       = r.IsDBNull(r.GetOrdinal("Skills")) ? string.Empty : r.GetString("Skills"),
        ResumeUrl    = r.IsDBNull(r.GetOrdinal("ResumeUrl")) ? string.Empty : r.GetString("ResumeUrl"),
        CreatedAt    = r.GetDateTime("CreatedAt"),
        UpdatedAt    = r.GetDateTime("UpdatedAt"),
    };
}
