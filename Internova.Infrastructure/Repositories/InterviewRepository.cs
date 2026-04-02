using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Internova.Infrastructure.Repositories;

public class InterviewRepository : IInterviewRepository
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<InterviewRepository> _logger;

    public InterviewRepository(DbConnectionFactory connectionFactory, ILogger<InterviewRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Interview> AddAsync(Interview interview)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO Interview (application_id, interview_date, location_or_link, created_at, updated_at)
            VALUES (@ApplicationId, @InterviewDate, @LocationOrLink, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ApplicationId", interview.ApplicationId);
        cmd.Parameters.AddWithValue("@InterviewDate", interview.InterviewDate);
        cmd.Parameters.AddWithValue("@LocationOrLink", interview.LocationOrLink);
        cmd.Parameters.AddWithValue("@CreatedAt", interview.CreatedAt);
        cmd.Parameters.AddWithValue("@UpdatedAt", interview.UpdatedAt);

        interview.Id = (int)await cmd.ExecuteScalarAsync();
        return interview;
    }

    public async Task<IEnumerable<Interview>> GetByStudentIdAsync(int studentId)
    {
        var interviews = new List<Interview>();
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT i.interview_id, i.application_id, i.interview_date, i.location_or_link, 
                   i.created_at, i.updated_at, intn.title, cp.company_name
            FROM Interview i
            JOIN Internship_Application a ON i.application_id = a.application_id
            JOIN Internship intn ON a.internship_id = intn.internship_id
            JOIN Company_Profile cp ON intn.company_id = cp.company_id
            WHERE a.student_id = @StudentId
            ORDER BY i.interview_date ASC";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@StudentId", studentId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            interviews.Add(new Interview
            {
                Id = reader.GetInt32(0),
                ApplicationId = reader.GetInt32(1),
                InterviewDate = reader.GetDateTime(2),
                LocationOrLink = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5),
                InternshipTitle = reader.GetString(6),
                CompanyName = reader.GetString(7)
            });
        }

        return interviews;
    }

    public async Task<IEnumerable<Interview>> GetByCompanyIdAsync(int companyId)
    {
        var interviews = new List<Interview>();
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT i.interview_id, i.application_id, i.interview_date, i.location_or_link, 
                   i.created_at, i.updated_at, intn.title, u.full_name
            FROM Interview i
            JOIN Internship_Application a ON i.application_id = a.application_id
            JOIN Internship intn ON a.internship_id = intn.internship_id
            JOIN [User] u ON a.student_id = u.user_id
            WHERE intn.company_id = @CompanyId
            ORDER BY i.interview_date ASC";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            interviews.Add(new Interview
            {
                Id = reader.GetInt32(0),
                ApplicationId = reader.GetInt32(1),
                InterviewDate = reader.GetDateTime(2),
                LocationOrLink = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5),
                InternshipTitle = reader.GetString(6),
                StudentName = reader.GetString(7)
            });
        }

        return interviews;
    }

    public async Task<Interview?> GetByApplicationIdAsync(int applicationId)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT interview_id, application_id, interview_date, location_or_link, 
                   created_at, updated_at
            FROM Interview
            WHERE application_id = @ApplicationId";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Interview
            {
                Id = reader.GetInt32(0),
                ApplicationId = reader.GetInt32(1),
                InterviewDate = reader.GetDateTime(2),
                LocationOrLink = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5)
            };
        }

        return null;
    }
}
