using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Internova.Infrastructure.Repositories;

/// <summary>
/// ADO.NET repository for StudentProfile using raw SqlClient queries.
/// Uses MERGE for a single-roundtrip upsert.
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
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT TOP 1 student_id, student_id AS user_id, university_id, department, gpa, skills, resume_link, created_at
            FROM dbo.Student_Profile
            WHERE student_id = @UserId;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapProfile(reader);
    }

    /// <summary>
    /// Inserts a new StudentProfile or updates the existing one if it already exists for the User.
    /// Leverages an atomic MERGE query to minimize round-trips.
    /// </summary>
    /// <param name="profile">The fully populated StudentProfile object to persist.</param>
    /// <returns>The freshly persisted profile as stored in the database.</returns>
    public async Task<StudentProfile> UpsertAsync(StudentProfile profile)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var now = DateTime.UtcNow;

        // Single-roundtrip upsert using SQL Server MERGE
        const string sql = """
            MERGE INTO dbo.Student_Profile AS Target
            USING (SELECT @UserId AS student_id) AS Source
            ON (Target.student_id = Source.student_id)
            WHEN MATCHED THEN
                UPDATE SET 
                    university_id = @UniversityId,
                    department    = @Department,
                    gpa           = @GPA,
                    skills        = @Skills,
                    resume_link   = @ResumeUrl
            WHEN NOT MATCHED THEN
                INSERT (student_id, university_id, department, gpa, skills, resume_link, created_at)
                VALUES (@UserId, @UniversityId, @Department, @GPA, @Skills, @ResumeUrl, @CreatedAt);

            SELECT TOP 1 student_id, student_id AS user_id, university_id, department, gpa, skills, resume_link, created_at
            FROM dbo.Student_Profile
            WHERE student_id = @UserId;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@UserId",       profile.UserId);
        cmd.Parameters.AddWithValue("@UniversityId", profile.UniversityId);
        cmd.Parameters.AddWithValue("@Department",   profile.Department);
        cmd.Parameters.AddWithValue("@GPA",          profile.GPA);
        cmd.Parameters.AddWithValue("@Skills",       profile.Skills);
        cmd.Parameters.AddWithValue("@ResumeUrl",    profile.ResumeUrl);
        cmd.Parameters.AddWithValue("@CreatedAt",    profile.CreatedAt == default ? now : profile.CreatedAt);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new Exception("Upsert failed to return a record.");

        var saved = MapProfile(reader);

        _logger.LogInformation("Upserted StudentProfile for UserId {UserId} → ProfileId {ProfileId}.",
            profile.UserId, saved.Id);

        return saved;
    }

    private static StudentProfile MapProfile(SqlDataReader r) => new()
    {
        Id           = r.GetInt32(r.GetOrdinal("student_id")),
        UserId       = r.GetInt32(r.GetOrdinal("user_id")),
        UniversityId = r.IsDBNull(r.GetOrdinal("university_id")) ? string.Empty : r.GetString(r.GetOrdinal("university_id")),
        Department   = r.IsDBNull(r.GetOrdinal("department")) ? string.Empty : r.GetString(r.GetOrdinal("department")),
        GPA          = r.IsDBNull(r.GetOrdinal("gpa")) ? 0 : r.GetDecimal(r.GetOrdinal("gpa")),
        Skills       = r.IsDBNull(r.GetOrdinal("skills")) ? string.Empty : r.GetString(r.GetOrdinal("skills")),
        ResumeUrl    = r.IsDBNull(r.GetOrdinal("resume_link")) ? string.Empty : r.GetString(r.GetOrdinal("resume_link")),
        CreatedAt    = r.GetDateTime(r.GetOrdinal("created_at")),
        UpdatedAt    = r.GetDateTime(r.GetOrdinal("created_at")), // Mocking UpdatedAt with CreatedAt
    };
}
