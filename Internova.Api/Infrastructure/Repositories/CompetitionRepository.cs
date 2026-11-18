using Dapper;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;

namespace Internova.Api.Infrastructure.Repositories;

public class CompetitionRepository(DbConnectionFactory connectionFactory) : ICompetitionRepository
{
    private const string SelectColumns = """
        SELECT c.competition_id AS Id,
               c.organizer_id AS OrganizerId,
               c.title AS Title,
               c.description AS Description,
               c.category AS Category,
               c.eligibility_criteria AS EligibilityCriteria,
               c.start_date AS StartDate,
               c.end_date AS EndDate,
               c.registration_link AS RegistrationLink,
               c.is_approved AS IsApproved,
               u.full_name AS OrganizerName
        FROM dbo.Competition c
        LEFT JOIN dbo.[User] u ON c.organizer_id = u.user_id
        """;

    public async Task<Competition?> GetByIdAsync(int id)
    {
        const string sql = $"""
            {SelectColumns}
            WHERE c.competition_id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Competition>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Competition>> GetAllAsync()
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<Competition>(SelectColumns);
    }

    public async Task<Competition> AddAsync(Competition competition)
    {
        const string sql = """
            INSERT INTO dbo.Competition (
                organizer_id, title, description, category, eligibility_criteria,
                start_date, end_date, registration_link, is_approved)
            VALUES (
                @OrganizerId, @Title, @Description, @Category, @EligibilityCriteria,
                @StartDate, @EndDate, @RegistrationLink, @IsApproved);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = connectionFactory.CreateConnection();
        competition.Id = await connection.ExecuteScalarAsync<int>(sql, competition);
        return competition;
    }

    public async Task<bool> UpdateAsync(Competition competition)
    {
        const string sql = """
            UPDATE dbo.Competition
            SET title = @Title,
                description = @Description,
                category = @Category,
                eligibility_criteria = @EligibilityCriteria,
                start_date = @StartDate,
                end_date = @EndDate,
                registration_link = @RegistrationLink,
                is_approved = @IsApproved
            WHERE competition_id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, competition);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM dbo.Competition WHERE competition_id = @Id;";

        await using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }
}
