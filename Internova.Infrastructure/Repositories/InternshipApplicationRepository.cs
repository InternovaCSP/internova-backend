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
            if (stats.ContainsKey(status))
            {
                stats[status] = count;
            }
        }

        return stats;
    }
}
