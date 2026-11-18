using Dapper;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Internova.Api.Infrastructure.Repositories;

public class InternshipRepository(
    DbConnectionFactory connectionFactory,
    ILogger<InternshipRepository> logger) : IInternshipRepository
{
    private const string SelectColumns = """
        SELECT i.internship_id AS Id,
               i.company_id AS CompanyId,
               i.title AS Title,
               i.description AS Description,
               i.duration AS Duration,
               i.location AS Location,
               i.requirements AS Requirements,
               i.status AS Status,
               i.is_published AS IsPublished,
               ISNULL(i.created_at, GETUTCDATE()) AS CreatedAt,
               cp.company_name AS CompanyName,
               i.company_description AS CompanyDescription
        FROM dbo.Internship i
        LEFT JOIN dbo.Company_Profile cp ON i.company_id = cp.company_id
        """;

    public async Task<Internship?> GetByIdAsync(int id)
    {
        const string sql = $"""
            {SelectColumns}
            WHERE i.internship_id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Internship>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Internship>> GetAllAsync()
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<Internship>(SelectColumns);
    }

    public async Task<IEnumerable<Internship>> GetByCompanyIdAsync(int companyId)
    {
        const string sql = $"""
            {SelectColumns}
            WHERE i.company_id = @CompanyId;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<Internship>(sql, new { CompanyId = companyId });
    }

    public async Task<Internship> AddAsync(Internship internship)
    {
        const string sql = """
            INSERT INTO dbo.Internship (
                company_id, title, description, duration, location, requirements, status, is_published, created_at)
            VALUES (
                @CompanyId, @Title, @Description, @Duration, @Location, @Requirements, @Status, @IsPublished, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = connectionFactory.CreateConnection();
        internship.Id = await connection.ExecuteScalarAsync<int>(sql, internship);
        return internship;
    }

    public async Task<bool> UpdateAsync(Internship internship)
    {
        const string sql = """
            UPDATE dbo.Internship
            SET title = @Title,
                description = @Description,
                duration = @Duration,
                location = @Location,
                requirements = @Requirements,
                status = @Status,
                is_published = @IsPublished
            WHERE internship_id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, internship);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            await connection.ExecuteAsync(
                "DELETE FROM dbo.Internship_Application WHERE internship_id = @Id;",
                new { Id = id },
                transaction);

            var affected = await connection.ExecuteAsync(
                "DELETE FROM dbo.Internship WHERE internship_id = @Id;",
                new { Id = id },
                transaction);

            await transaction.CommitAsync();
            return affected > 0;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error deleting internship {InternshipId}", id);
            throw;
        }
    }
}
