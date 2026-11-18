using Dapper;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Internova.Api.Infrastructure.Repositories;

/// <summary>
/// Dapper repository for StudentProfile persistence using single-roundtrip SQL MERGE upsert.
/// </summary>
public class StudentProfileRepository(
    DbConnectionFactory connectionFactory,
    ILogger<StudentProfileRepository> logger) : IStudentProfileRepository
{
    private const string SelectColumns = """
        student_id AS Id,
        student_id AS UserId,
        ISNULL(university_id, '') AS UniversityId,
        ISNULL(department, '') AS Department,
        ISNULL(gpa, 0) AS GPA,
        ISNULL(skills, '') AS Skills,
        ISNULL(resume_link, '') AS ResumeUrl,
        created_at AS CreatedAt,
        created_at AS UpdatedAt
        """;

    public async Task<StudentProfile?> GetByUserIdAsync(int userId)
    {
        const string sql = $"""
            SELECT TOP 1 {SelectColumns}
            FROM dbo.Student_Profile
            WHERE student_id = @UserId;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<StudentProfile>(sql, new { UserId = userId });
    }

    public async Task<StudentProfile> UpsertAsync(StudentProfile profile)
    {
        const string sql = $"""
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

            SELECT TOP 1 {SelectColumns}
            FROM dbo.Student_Profile
            WHERE student_id = @UserId;
            """;

        await using var connection = connectionFactory.CreateConnection();
        var saved = await connection.QuerySingleAsync<StudentProfile>(sql, new
        {
            profile.UserId,
            profile.UniversityId,
            profile.Department,
            profile.GPA,
            profile.Skills,
            profile.ResumeUrl,
            CreatedAt = profile.CreatedAt == default ? DateTime.UtcNow : profile.CreatedAt
        });

        logger.LogInformation("Upserted StudentProfile for UserId {UserId} → ProfileId {ProfileId}.",
            profile.UserId, saved.Id);

        return saved;
    }
}
