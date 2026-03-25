using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Internova.Infrastructure.Repositories;

public class CompetitionRepository : ICompetitionRepository
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<CompetitionRepository> _logger;

    public CompetitionRepository(DbConnectionFactory connectionFactory, ILogger<CompetitionRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Competition?> GetByIdAsync(int id)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT c.competition_id AS Id, c.organizer_id AS OrganizerId, c.title AS Title, 
                   c.description AS Description, c.category AS Category, 
                   c.eligibility_criteria AS EligibilityCriteria, c.start_date AS StartDate, 
                   c.end_date AS EndDate, c.registration_link AS RegistrationLink, 
                   c.is_approved AS IsApproved, u.full_name AS OrganizerName
            FROM dbo.Competition c
            LEFT JOIN dbo.[User] u ON c.organizer_id = u.user_id
            WHERE c.competition_id = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapCompetition(reader);
    }

    public async Task<IEnumerable<Competition>> GetAllAsync()
    {
        var competitions = new List<Competition>();
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT c.competition_id AS Id, c.organizer_id AS OrganizerId, c.title AS Title, 
                   c.description AS Description, c.category AS Category, 
                   c.eligibility_criteria AS EligibilityCriteria, c.start_date AS StartDate, 
                   c.end_date AS EndDate, c.registration_link AS RegistrationLink, 
                   c.is_approved AS IsApproved, u.full_name AS OrganizerName
            FROM dbo.Competition c
            LEFT JOIN dbo.[User] u ON c.organizer_id = u.user_id";

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            competitions.Add(MapCompetition(reader));
        }

        return competitions;
    }

    public async Task<Competition> AddAsync(Competition competition)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO dbo.Competition (organizer_id, title, description, category, eligibility_criteria, start_date, end_date, registration_link, is_approved)
            VALUES (@OrganizerId, @Title, @Description, @Category, @EligibilityCriteria, @StartDate, @EndDate, @RegistrationLink, @IsApproved);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@OrganizerId", competition.OrganizerId);
        cmd.Parameters.AddWithValue("@Title", competition.Title);
        cmd.Parameters.AddWithValue("@Description", (object?)competition.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Category", (object?)competition.Category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EligibilityCriteria", (object?)competition.EligibilityCriteria ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StartDate", (object?)competition.StartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EndDate", (object?)competition.EndDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RegistrationLink", (object?)competition.RegistrationLink ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsApproved", competition.IsApproved);

        competition.Id = (int)await cmd.ExecuteScalarAsync();
        return competition;
    }

    public async Task<bool> UpdateAsync(Competition competition)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            UPDATE dbo.Competition
            SET title = @Title,
                description = @Description,
                category = @Category,
                eligibility_criteria = @EligibilityCriteria,
                start_date = @StartDate,
                end_date = @EndDate,
                registration_link = @RegistrationLink,
                is_approved = @IsApproved
            WHERE competition_id = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", competition.Id);
        cmd.Parameters.AddWithValue("@Title", competition.Title);
        cmd.Parameters.AddWithValue("@Description", (object?)competition.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Category", (object?)competition.Category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EligibilityCriteria", (object?)competition.EligibilityCriteria ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StartDate", (object?)competition.StartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EndDate", (object?)competition.EndDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RegistrationLink", (object?)competition.RegistrationLink ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsApproved", competition.IsApproved);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "DELETE FROM dbo.Competition WHERE competition_id = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    private static Competition MapCompetition(SqlDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        OrganizerId = r.GetInt32(r.GetOrdinal("OrganizerId")),
        Title = r.GetString(r.GetOrdinal("Title")),
        Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
        Category = r.IsDBNull(r.GetOrdinal("Category")) ? null : r.GetString(r.GetOrdinal("Category")),
        EligibilityCriteria = r.IsDBNull(r.GetOrdinal("EligibilityCriteria")) ? null : r.GetString(r.GetOrdinal("EligibilityCriteria")),
        StartDate = r.IsDBNull(r.GetOrdinal("StartDate")) ? null : r.GetDateTime(r.GetOrdinal("StartDate")),
        EndDate = r.IsDBNull(r.GetOrdinal("EndDate")) ? null : r.GetDateTime(r.GetOrdinal("EndDate")),
        RegistrationLink = r.IsDBNull(r.GetOrdinal("RegistrationLink")) ? null : r.GetString(r.GetOrdinal("RegistrationLink")),
        IsApproved = r.GetBoolean(r.GetOrdinal("IsApproved")),
        OrganizerName = r.IsDBNull(r.GetOrdinal("OrganizerName")) ? null : r.GetString(r.GetOrdinal("OrganizerName"))
    };
}
