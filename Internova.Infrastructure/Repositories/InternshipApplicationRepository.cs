using Internova.Core.Entities;
using Internova.Core.Enums;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Internova.Infrastructure.Repositories;

public class InternshipApplicationRepository : IInternshipApplicationRepository
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<InternshipApplicationRepository> _logger;

    public InternshipApplicationRepository(DbConnectionFactory connectionFactory, ILogger<InternshipApplicationRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<InternshipApplication> AddAsync(InternshipApplication application)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO Internship_Application (internship_id, student_id, status, applied_at, updated_at)
            VALUES (@InternshipId, @StudentId, @Status, @AppliedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@InternshipId", application.InternshipId);
        cmd.Parameters.AddWithValue("@StudentId", application.StudentId);
        cmd.Parameters.AddWithValue("@Status", application.Status.ToString());
        cmd.Parameters.AddWithValue("@AppliedAt", application.AppliedAt);
        cmd.Parameters.AddWithValue("@UpdatedAt", application.UpdatedAt);

        application.Id = (int)await cmd.ExecuteScalarAsync();
        return application;
    }

    public async Task<IEnumerable<InternshipApplication>> GetByStudentIdAsync(int studentId)
    {
        var applications = new List<InternshipApplication>();
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT a.application_id as Id, a.internship_id as InternshipId, a.student_id as StudentId, 
                   a.status as Status, a.applied_at as AppliedAt, a.updated_at as UpdatedAt,
                   i.title as InternshipTitle, cp.company_name as CompanyName
            FROM Internship_Application a
            JOIN Internship i ON a.internship_id = i.internship_id
            JOIN Company_Profile cp ON i.company_id = cp.company_id
            WHERE a.student_id = @StudentId";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@StudentId", studentId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            applications.Add(new InternshipApplication
            {
                Id = reader.GetInt32(0),
                InternshipId = reader.GetInt32(1),
                StudentId = reader.GetInt32(2),
                Status = Enum.Parse<ApplicationStatus>(reader.GetString(3)),
                AppliedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5),
                InternshipTitle = reader.GetString(6),
                CompanyName = reader.GetString(7)
            });
        }

        return applications;
    }

    public async Task<IEnumerable<InternshipApplication>> GetByCompanyIdAsync(int companyId)
    {
        var applications = new List<InternshipApplication>();
        try
        {
            await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT a.application_id as Id, a.internship_id as InternshipId, a.student_id as StudentId, 
                       a.status as Status, a.applied_at as AppliedAt, a.updated_at as UpdatedAt,
                       i.title as InternshipTitle, u.full_name as StudentName
                FROM Internship_Application a
                JOIN Internship i ON a.internship_id = i.internship_id
                JOIN [User] u ON a.student_id = u.user_id
                WHERE i.company_id = @CompanyId";

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@CompanyId", companyId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var statusStr = reader.GetString(3);
                if (!Enum.TryParse<ApplicationStatus>(statusStr, true, out var status))
                {
                    _logger.LogWarning("Invalid status '{Status}' for application {Id}", statusStr, reader.GetInt32(0));
                    status = ApplicationStatus.Applied; // Fallback
                }

                applications.Add(new InternshipApplication
                {
                    Id = reader.GetInt32(0),
                    InternshipId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Status = status,
                    AppliedAt = reader.GetDateTime(4),
                    UpdatedAt = reader.GetDateTime(5),
                    InternshipTitle = reader.GetString(6),
                    StudentName = reader.IsDBNull(7) ? "Unknown Student" : reader.GetString(7)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error in GetByCompanyIdAsync for company {CompanyId}", companyId);
            throw;
        }

        return applications;
    }

    public async Task<bool> UpdateStatusAsync(int applicationId, ApplicationStatus status)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            UPDATE Internship_Application 
            SET status = @Status, updated_at = GETDATE() 
            WHERE application_id = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Status", status.ToString());
        cmd.Parameters.AddWithValue("@Id", applicationId);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<IDictionary<string, int>> GetPipelineStatsAsync(int studentId)
    {
        var stats = new Dictionary<string, int>
        {
            { "Applied", 0 },
            { "Shortlisted", 0 },
            { "Interviewing", 0 },
            { "Selected", 0 }
        };

        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT status, COUNT(*) 
            FROM Internship_Application 
            WHERE student_id = @StudentId AND status != 'Rejected'
            GROUP BY status";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@StudentId", studentId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var status = reader.GetString(0);
            var count = reader.GetInt32(1);

            if (status == "InterviewScheduled")
            {
                stats["Interviewing"] += count;
            }
            else if (stats.ContainsKey(status))
            {
                stats[status] = count;
            }
        }

        return stats;
    }

    public async Task<IDictionary<string, string>> GetKpiStatsAsync(int studentId)
    {
        var stats = new Dictionary<string, string>
        {
            { "Applications", "0" },
            { "Interviews", "0" }
        };

        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                COUNT(*) as TotalApplications,
                SUM(CASE WHEN status IN ('Interviewing', 'InterviewScheduled') THEN 1 ELSE 0 END) as InterviewCount
            FROM Internship_Application 
            WHERE student_id = @StudentId";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@StudentId", studentId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            stats["Applications"] = reader.GetInt32(0).ToString();
            stats["Interviews"] = (reader.IsDBNull(1) ? 0 : reader.GetInt32(1)).ToString();
        }

        return stats;
    }
}
