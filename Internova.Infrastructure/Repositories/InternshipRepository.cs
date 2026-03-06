using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Internova.Infrastructure.Repositories;

public class InternshipRepository : IInternshipRepository
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<InternshipRepository> _logger;

    public InternshipRepository(DbConnectionFactory connectionFactory, ILogger<InternshipRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Internship?> GetByIdAsync(int id)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT internship_id AS Id, company_id AS CompanyId, title AS Title, 
                   description AS Description, duration AS Type, location AS Location, 
                   0.0 AS Stipend, requirements AS Skills, created_at AS CreatedAt
            FROM dbo.Internship
            WHERE internship_id = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapInternship(reader);
    }

    public async Task<IEnumerable<Internship>> GetAllAsync()
    {
        var internships = new List<Internship>();
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT internship_id AS Id, company_id AS CompanyId, title AS Title, 
                   description AS Description, duration AS Type, location AS Location, 
                   0.0 AS Stipend, requirements AS Skills, created_at AS CreatedAt
            FROM dbo.Internship";

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            internships.Add(MapInternship(reader));
        }

        return internships;
    }

    public async Task<Internship> AddAsync(Internship internship)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO dbo.Internship (company_id, title, description, duration, location, requirements, created_at)
            VALUES (@CompanyId, @Title, @Description, @Type, @Location, @Skills, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CompanyId", internship.CompanyId);
        cmd.Parameters.AddWithValue("@Title", internship.Title);
        cmd.Parameters.AddWithValue("@Description", internship.Description);
        cmd.Parameters.AddWithValue("@Type", internship.Type);
        cmd.Parameters.AddWithValue("@Location", internship.Location);
        cmd.Parameters.AddWithValue("@Skills", internship.Skills);
        cmd.Parameters.AddWithValue("@CreatedAt", internship.CreatedAt);

        internship.Id = (int)await cmd.ExecuteScalarAsync();
        return internship;
    }

    public async Task<bool> UpdateAsync(Internship internship)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            UPDATE dbo.Internship
            SET title = @Title,
                description = @Description,
                duration = @Type,
                location = @Location,
                requirements = @Skills
            WHERE internship_id = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", internship.Id);
        cmd.Parameters.AddWithValue("@Title", internship.Title);
        cmd.Parameters.AddWithValue("@Description", internship.Description);
        cmd.Parameters.AddWithValue("@Type", internship.Type);
        cmd.Parameters.AddWithValue("@Location", internship.Location);
        cmd.Parameters.AddWithValue("@Skills", internship.Skills);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "DELETE FROM dbo.Internship WHERE internship_id = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    private static Internship MapInternship(SqlDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        CompanyId = r.GetInt32(r.GetOrdinal("CompanyId")),
        Title = r.GetString(r.GetOrdinal("Title")),
        Description = r.IsDBNull(r.GetOrdinal("Description")) ? string.Empty : r.GetString(r.GetOrdinal("Description")),
        Type = r.IsDBNull(r.GetOrdinal("Type")) ? string.Empty : r.GetString(r.GetOrdinal("Type")),
        Location = r.IsDBNull(r.GetOrdinal("Location")) ? string.Empty : r.GetString(r.GetOrdinal("Location")),
        Stipend = null, // Database schema in azure_sql.txt doesn't have stipend yet, will use 0.0 or nullable
        Skills = r.IsDBNull(r.GetOrdinal("Skills")) ? string.Empty : r.GetString(r.GetOrdinal("Skills")),
        CreatedAt = r.IsDBNull(r.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : r.GetDateTime(r.GetOrdinal("CreatedAt"))
    };
}
