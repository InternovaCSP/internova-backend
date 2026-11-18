using Dapper;
using Internova.Api.Domain.Entities;
using Internova.Api.Domain.Enums;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Internova.Api.Infrastructure.Repositories;

public class InternshipApplicationRepository(
    DbConnectionFactory connectionFactory,
    ILogger<InternshipApplicationRepository> logger) : IInternshipApplicationRepository
{
    public async Task<InternshipApplication> AddAsync(InternshipApplication application)
    {
        const string sql = """
            INSERT INTO dbo.Internship_Application (internship_id, student_id, status, applied_at, updated_at)
            VALUES (@InternshipId, @StudentId, @Status, @AppliedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = connectionFactory.CreateConnection();
        application.Id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            application.InternshipId,
            application.StudentId,
            Status = application.Status.ToString(),
            application.AppliedAt,
            application.UpdatedAt
        });

        return application;
    }

    public async Task<IEnumerable<InternshipApplication>> GetByStudentIdAsync(int studentId)
    {
        const string sql = """
            SELECT a.application_id AS Id,
                   a.internship_id AS InternshipId,
                   a.student_id AS StudentId,
                   a.status AS Status,
                   a.applied_at AS AppliedAt,
                   a.updated_at AS UpdatedAt,
                   i.title AS InternshipTitle,
                   cp.company_name AS CompanyName
            FROM dbo.Internship_Application a
            JOIN dbo.Internship i ON a.internship_id = i.internship_id
            JOIN dbo.Company_Profile cp ON i.company_id = cp.company_id
            WHERE a.student_id = @StudentId;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<InternshipApplication>(sql, new { StudentId = studentId });
    }

    public async Task<IEnumerable<InternshipApplication>> GetByCompanyIdAsync(int companyId)
    {
        const string sql = """
            SELECT a.application_id AS Id,
                   a.internship_id AS InternshipId,
                   a.student_id AS StudentId,
                   a.status AS StatusText,
                   a.applied_at AS AppliedAt,
                   a.updated_at AS UpdatedAt,
                   i.title AS InternshipTitle,
                   ISNULL(u.full_name, 'Unknown Student') AS StudentName
            FROM dbo.Internship_Application a
            JOIN dbo.Internship i ON a.internship_id = i.internship_id
            JOIN dbo.[User] u ON a.student_id = u.user_id
            WHERE i.company_id = @CompanyId;
            """;

        await using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<CompanyApplicationRow>(sql, new { CompanyId = companyId });

        return rows.Select(r =>
        {
            if (!Enum.TryParse<ApplicationStatus>(r.StatusText, true, out var status))
            {
                logger.LogWarning("Invalid status '{Status}' for application {Id}", r.StatusText, r.Id);
                status = ApplicationStatus.Applied;
            }

            return new InternshipApplication
            {
                Id = r.Id,
                InternshipId = r.InternshipId,
                StudentId = r.StudentId,
                Status = status,
                AppliedAt = r.AppliedAt,
                UpdatedAt = r.UpdatedAt,
                InternshipTitle = r.InternshipTitle,
                StudentName = r.StudentName
            };
        });
    }

    public async Task<bool> UpdateStatusAsync(int applicationId, ApplicationStatus status)
    {
        const string sql = """
            UPDATE dbo.Internship_Application
            SET status = @Status, updated_at = GETUTCDATE()
            WHERE application_id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, new
        {
            Id = applicationId,
            Status = status.ToString()
        });
        return affected > 0;
    }

    public async Task<IDictionary<string, int>> GetPipelineStatsAsync(int studentId)
    {
        var stats = new Dictionary<string, int>
        {
            ["Applied"] = 0,
            ["Shortlisted"] = 0,
            ["Interviewing"] = 0,
            ["Selected"] = 0
        };

        const string sql = """
            SELECT status AS Status, COUNT(*) AS Count
            FROM dbo.Internship_Application
            WHERE student_id = @StudentId AND status != 'Rejected'
            GROUP BY status;
            """;

        await using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<(string Status, int Count)>(sql, new { StudentId = studentId });

        foreach (var (status, count) in rows)
        {
            if (status == "InterviewScheduled")
                stats["Interviewing"] += count;
            else if (stats.ContainsKey(status))
                stats[status] = count;
        }

        return stats;
    }

    public async Task<IDictionary<string, string>> GetKpiStatsAsync(int studentId)
    {
        const string sql = """
            SELECT
                COUNT(*) AS TotalApplications,
                ISNULL(SUM(CASE WHEN status IN ('Interviewing', 'InterviewScheduled') THEN 1 ELSE 0 END), 0) AS InterviewCount
            FROM dbo.Internship_Application
            WHERE student_id = @StudentId;
            """;

        await using var connection = connectionFactory.CreateConnection();
        var (total, interviews) = await connection.QuerySingleAsync<(int Total, int Interviews)>(
            sql, new { StudentId = studentId });

        return new Dictionary<string, string>
        {
            ["Applications"] = total.ToString(),
            ["Interviews"] = interviews.ToString()
        };
    }

    private sealed record CompanyApplicationRow(
        int Id,
        int InternshipId,
        int StudentId,
        string StatusText,
        DateTime AppliedAt,
        DateTime UpdatedAt,
        string InternshipTitle,
        string StudentName);
}
