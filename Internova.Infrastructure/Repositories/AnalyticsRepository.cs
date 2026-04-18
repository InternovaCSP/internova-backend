using Internova.Core.DTOs;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Internova.Infrastructure.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<AnalyticsRepository> _logger;

    public AnalyticsRepository(DbConnectionFactory connectionFactory, ILogger<AnalyticsRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<AdminStatsDto> GetAdminStatsAsync()
    {
        var stats = new AdminStatsDto();
        
        try
        {
            await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();

            // 1. Placed Count: Students with at least one 'Selected' status
            const string placedSql = @"
                SELECT COUNT(DISTINCT student_id) 
                FROM Internship_Application 
                WHERE status = 'Selected'";
            
            await using var placedCmd = new SqlCommand(placedSql, connection);
            stats.Placed = (int)await placedCmd.ExecuteScalarAsync()!;

            // 2. Seeking Count: Students (role='Student') who are NOT placed
            const string seekingSql = @"
                SELECT COUNT(*) 
                FROM [User] u
                WHERE u.role = 'Student'
                  AND NOT EXISTS (
                    SELECT 1 
                    FROM Internship_Application a 
                    WHERE a.student_id = u.user_id 
                      AND a.status = 'Selected'
                  )";
            
            await using var seekingCmd = new SqlCommand(seekingSql, connection);
            stats.Seeking = (int)await seekingCmd.ExecuteScalarAsync()!;

            // 3. Industries Distribution: Count internships per industry
            const string industrySql = @"
                SELECT ISNULL(cp.industry, 'Other') as Name, COUNT(*) as Count
                FROM Internship i
                LEFT JOIN Company_Profile cp ON i.company_id = cp.company_id
                GROUP BY ISNULL(cp.industry, 'Other')";

            await using var industryCmd = new SqlCommand(industrySql, connection);
            await using var reader = await industryCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stats.Industries.Add(new IndustryStatsDto
                {
                    Name = reader.GetString(0),
                    Count = reader.GetInt32(1)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching admin analytics stats.");
            throw;
        }

        return stats;
    }
}
