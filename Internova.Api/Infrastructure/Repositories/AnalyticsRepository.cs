using Dapper;
using Internova.Api.Application.DTOs;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;

namespace Internova.Api.Infrastructure.Repositories;

public class AnalyticsRepository(DbConnectionFactory connectionFactory) : IAnalyticsRepository
{
    public async Task<AdminStatsDto> GetAdminStatsAsync()
    {
        const string sql = """
            SELECT COUNT(DISTINCT student_id)
            FROM dbo.Internship_Application
            WHERE status = 'Selected';

            SELECT COUNT(*)
            FROM dbo.[User] u
            WHERE u.role = 'Student'
              AND NOT EXISTS (
                  SELECT 1
                  FROM dbo.Internship_Application a
                  WHERE a.student_id = u.user_id
                    AND a.status = 'Selected'
              );

            SELECT ISNULL(cp.industry, 'Other') AS Name, COUNT(*) AS Count
            FROM dbo.Internship i
            LEFT JOIN dbo.Company_Profile cp ON i.company_id = cp.company_id
            GROUP BY ISNULL(cp.industry, 'Other');
            """;

        await using var connection = connectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql);

        return new AdminStatsDto
        {
            Placed = await multi.ReadSingleAsync<int>(),
            Seeking = await multi.ReadSingleAsync<int>(),
            Industries = (await multi.ReadAsync<IndustryStatsDto>()).ToList()
        };
    }
}
