using Dapper;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;

namespace Internova.Api.Infrastructure.Repositories;

public class InterviewRepository(DbConnectionFactory connectionFactory) : IInterviewRepository
{
    public async Task<Interview> AddAsync(Interview interview)
    {
        const string sql = """
            INSERT INTO dbo.Interview (application_id, interview_date, location_or_link, created_at, updated_at)
            VALUES (@ApplicationId, @InterviewDate, @LocationOrLink, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = connectionFactory.CreateConnection();
        interview.Id = await connection.ExecuteScalarAsync<int>(sql, interview);
        return interview;
    }

    public async Task<IEnumerable<Interview>> GetByStudentIdAsync(int studentId)
    {
        const string sql = """
            SELECT i.interview_id AS Id,
                   i.application_id AS ApplicationId,
                   i.interview_date AS InterviewDate,
                   i.location_or_link AS LocationOrLink,
                   i.created_at AS CreatedAt,
                   i.updated_at AS UpdatedAt,
                   intn.title AS InternshipTitle,
                   cp.company_name AS CompanyName
            FROM dbo.Interview i
            JOIN dbo.Internship_Application a ON i.application_id = a.application_id
            JOIN dbo.Internship intn ON a.internship_id = intn.internship_id
            JOIN dbo.Company_Profile cp ON intn.company_id = cp.company_id
            WHERE a.student_id = @StudentId
            ORDER BY i.interview_date ASC;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<Interview>(sql, new { StudentId = studentId });
    }

    public async Task<IEnumerable<Interview>> GetByCompanyIdAsync(int companyId)
    {
        const string sql = """
            SELECT i.interview_id AS Id,
                   i.application_id AS ApplicationId,
                   i.interview_date AS InterviewDate,
                   i.location_or_link AS LocationOrLink,
                   i.created_at AS CreatedAt,
                   i.updated_at AS UpdatedAt,
                   intn.title AS InternshipTitle,
                   u.full_name AS StudentName
            FROM dbo.Interview i
            JOIN dbo.Internship_Application a ON i.application_id = a.application_id
            JOIN dbo.Internship intn ON a.internship_id = intn.internship_id
            JOIN dbo.[User] u ON a.student_id = u.user_id
            WHERE intn.company_id = @CompanyId
            ORDER BY i.interview_date ASC;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<Interview>(sql, new { CompanyId = companyId });
    }

    public async Task<Interview?> GetByApplicationIdAsync(int applicationId)
    {
        const string sql = """
            SELECT interview_id AS Id,
                   application_id AS ApplicationId,
                   interview_date AS InterviewDate,
                   location_or_link AS LocationOrLink,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM dbo.Interview
            WHERE application_id = @ApplicationId;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Interview>(sql, new { ApplicationId = applicationId });
    }
}
