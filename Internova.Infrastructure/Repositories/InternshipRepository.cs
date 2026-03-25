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
            SELECT i.internship_id AS Id, i.company_id AS CompanyId, i.title AS Title, 
                   i.description AS Description, i.duration AS Duration, i.location AS Location, 
                   i.requirements AS Requirements, i.status AS Status, i.is_published AS IsPublished, 
                   i.created_at AS CreatedAt, cp.company_name AS CompanyName, i.company_description AS CompanyDescription
            FROM dbo.Internship i
            LEFT JOIN dbo.Company_Profile cp ON i.company_id = cp.company_id
            WHERE i.internship_id = @Id";

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
            SELECT i.internship_id AS Id, i.company_id AS CompanyId, i.title AS Title, 
                   i.description AS Description, i.duration AS Duration, i.location AS Location, 
                   i.requirements AS Requirements, i.status AS Status, i.is_published AS IsPublished, 
                   i.created_at AS CreatedAt, cp.company_name AS CompanyName, i.company_description AS CompanyDescription
            FROM dbo.Internship i
            LEFT JOIN dbo.Company_Profile cp ON i.company_id = cp.company_id";

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            internships.Add(MapInternship(reader));
        }

        return internships;
    }

    public async Task<IEnumerable<Internship>> GetByCompanyIdAsync(int companyId)
    {
        var internships = new List<Internship>();
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT i.internship_id AS Id, i.company_id AS CompanyId, i.title AS Title, 
                   i.description AS Description, i.duration AS Duration, i.location AS Location, 
                   i.requirements AS Requirements, i.status AS Status, i.is_published AS IsPublished, 
                   i.created_at AS CreatedAt, cp.company_name AS CompanyName, i.company_description AS CompanyDescription
            FROM dbo.Internship i
            LEFT JOIN dbo.Company_Profile cp ON i.company_id = cp.company_id
            WHERE i.company_id = @CompanyId";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);

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
            INSERT INTO dbo.Internship (company_id, title, description, duration, location, requirements, status, is_published, created_at)
            VALUES (@CompanyId, @Title, @Description, @Duration, @Location, @Requirements, @Status, @IsPublished, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CompanyId", internship.CompanyId);
        cmd.Parameters.AddWithValue("@Title", internship.Title);
        cmd.Parameters.AddWithValue("@Description", (object?)internship.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Duration", (object?)internship.Duration ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Location", (object?)internship.Location ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Requirements", (object?)internship.Requirements ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", internship.Status);
        cmd.Parameters.AddWithValue("@IsPublished", internship.IsPublished);
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
                duration = @Duration,
                location = @Location,
                requirements = @Requirements,
                status = @Status,
                is_published = @IsPublished
            WHERE internship_id = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", internship.Id);
        cmd.Parameters.AddWithValue("@Title", internship.Title);
        cmd.Parameters.AddWithValue("@Description", (object?)internship.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Duration", (object?)internship.Duration ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Location", (object?)internship.Location ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Requirements", (object?)internship.Requirements ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", internship.Status);
        cmd.Parameters.AddWithValue("@IsPublished", internship.IsPublished);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Delete associated applications
            const string deleteAppsSql = "DELETE FROM dbo.Internship_Application WHERE internship_id = @Id";
            await using var deleteAppsCmd = new SqlCommand(deleteAppsSql, connection, transaction);
            deleteAppsCmd.Parameters.AddWithValue("@Id", id);
            await deleteAppsCmd.ExecuteNonQueryAsync();

            // 2. Delete the internship
            const string deleteInternshipSql = "DELETE FROM dbo.Internship WHERE internship_id = @Id";
            await using var deleteInternshipCmd = new SqlCommand(deleteInternshipSql, connection, transaction);
            deleteInternshipCmd.Parameters.AddWithValue("@Id", id);

            var affected = await deleteInternshipCmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return affected > 0;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error deleting internship {InternshipId}", id);
            throw;
        }
    }

    private static Internship MapInternship(SqlDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        CompanyId = r.GetInt32(r.GetOrdinal("CompanyId")),
        Title = r.GetString(r.GetOrdinal("Title")),
        Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
        Duration = r.IsDBNull(r.GetOrdinal("Duration")) ? null : r.GetString(r.GetOrdinal("Duration")),
        Location = r.IsDBNull(r.GetOrdinal("Location")) ? null : r.GetString(r.GetOrdinal("Location")),
        Requirements = r.IsDBNull(r.GetOrdinal("Requirements")) ? null : r.GetString(r.GetOrdinal("Requirements")),
        Status = r.GetString(r.GetOrdinal("Status")),
        IsPublished = r.GetBoolean(r.GetOrdinal("IsPublished")),
        CompanyName = r.IsDBNull(r.GetOrdinal("CompanyName")) ? null : r.GetString(r.GetOrdinal("CompanyName")),
        CompanyDescription = r.IsDBNull(r.GetOrdinal("CompanyDescription")) ? null : r.GetString(r.GetOrdinal("CompanyDescription")),
        CreatedAt = r.IsDBNull(r.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : r.GetDateTime(r.GetOrdinal("CreatedAt"))
    };
}
