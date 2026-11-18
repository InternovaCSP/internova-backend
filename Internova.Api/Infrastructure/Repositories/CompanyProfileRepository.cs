using Dapper;
using Internova.Api.Domain.Entities;
using Internova.Api.Domain.Enums;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;

namespace Internova.Api.Infrastructure.Repositories;

public class CompanyProfileRepository(DbConnectionFactory connectionFactory) : ICompanyProfileRepository
{
    private const string SelectColumns = """
        company_id AS CompanyId,
        company_name AS CompanyName,
        industry AS Industry,
        address AS Address,
        description AS Description,
        website_url AS WebsiteUrl,
        is_verified AS IsVerified,
        status AS Status
        """;

    public async Task<CompanyProfile?> GetByCompanyIdAsync(int companyId)
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM dbo.Company_Profile
            WHERE company_id = @CompanyId;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<CompanyProfile>(sql, new { CompanyId = companyId });
    }

    public async Task<IEnumerable<CompanyProfile>> GetPendingCompaniesAsync()
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM dbo.Company_Profile
            WHERE status = 'Pending';
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<CompanyProfile>(sql);
    }

    public async Task<IEnumerable<CompanyProfile>> GetAllCompaniesAsync()
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM dbo.Company_Profile;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<CompanyProfile>(sql);
    }

    public async Task<bool> UpdateStatusAsync(int companyId, CompanyStatus status)
    {
        const string sql = """
            UPDATE dbo.Company_Profile
            SET status = @Status
            WHERE company_id = @CompanyId;
            """;

        await using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, new
        {
            CompanyId = companyId,
            Status = status.ToString()
        });
        return affected > 0;
    }

    public async Task<CompanyProfile> AddAsync(CompanyProfile profile)
    {
        const string sql = """
            INSERT INTO dbo.Company_Profile (
                company_id, company_name, industry, address, description, website_url, is_verified, status)
            VALUES (
                @CompanyId, @CompanyName, @Industry, @Address, @Description, @WebsiteUrl, @IsVerified, @Status);
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            profile.CompanyId,
            profile.CompanyName,
            profile.Industry,
            profile.Address,
            profile.Description,
            profile.WebsiteUrl,
            profile.IsVerified,
            Status = profile.Status.ToString()
        });

        return profile;
    }
}
