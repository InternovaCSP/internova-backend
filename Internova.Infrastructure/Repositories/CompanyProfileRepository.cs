using Internova.Core.Entities;
using Internova.Core.Enums;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Internova.Infrastructure.Repositories;

public class CompanyProfileRepository : ICompanyProfileRepository
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<CompanyProfileRepository> _logger;

    public CompanyProfileRepository(DbConnectionFactory connectionFactory, ILogger<CompanyProfileRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<CompanyProfile?> GetByCompanyIdAsync(int companyId)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT company_id AS CompanyId, company_name AS CompanyName, industry AS Industry, 
                   address AS Address, description AS Description, website_url AS WebsiteUrl, 
                   is_verified AS IsVerified, status AS Status
            FROM dbo.Company_Profile
            WHERE company_id = @CompanyId";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapCompanyProfile(reader);
    }

    public async Task<IEnumerable<CompanyProfile>> GetPendingCompaniesAsync()
    {
        var companies = new List<CompanyProfile>();
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT company_id AS CompanyId, company_name AS CompanyName, industry AS Industry, 
                   address AS Address, description AS Description, website_url AS WebsiteUrl, 
                   is_verified AS IsVerified, status AS Status
            FROM dbo.Company_Profile
            WHERE status = 'Pending'";

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            companies.Add(MapCompanyProfile(reader));
        }

        return companies;
    }

    public async Task<bool> UpdateStatusAsync(int companyId, CompanyStatus status)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            UPDATE dbo.Company_Profile
            SET status = @Status
            WHERE company_id = @CompanyId";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        cmd.Parameters.AddWithValue("@Status", status.ToString());

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<CompanyProfile> AddAsync(CompanyProfile profile)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO dbo.Company_Profile (company_id, company_name, industry, address, description, website_url, is_verified, status)
            VALUES (@CompanyId, @CompanyName, @Industry, @Address, @Description, @WebsiteUrl, @IsVerified, @Status)";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CompanyId", profile.CompanyId);
        cmd.Parameters.AddWithValue("@CompanyName", profile.CompanyName);
        cmd.Parameters.AddWithValue("@Industry", (object?)profile.Industry ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Address", (object?)profile.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", (object?)profile.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@WebsiteUrl", (object?)profile.WebsiteUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsVerified", profile.IsVerified);
        cmd.Parameters.AddWithValue("@Status", profile.Status.ToString());

        await cmd.ExecuteNonQueryAsync();
        return profile;
    }

    private static CompanyProfile MapCompanyProfile(SqlDataReader r) => new()
    {
        CompanyId = r.GetInt32(r.GetOrdinal("CompanyId")),
        CompanyName = r.GetString(r.GetOrdinal("CompanyName")),
        Industry = r.IsDBNull(r.GetOrdinal("Industry")) ? null : r.GetString(r.GetOrdinal("Industry")),
        Address = r.IsDBNull(r.GetOrdinal("Address")) ? null : r.GetString(r.GetOrdinal("Address")),
        Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
        WebsiteUrl = r.IsDBNull(r.GetOrdinal("WebsiteUrl")) ? null : r.GetString(r.GetOrdinal("WebsiteUrl")),
        IsVerified = r.GetBoolean(r.GetOrdinal("IsVerified")),
        Status = Enum.Parse<CompanyStatus>(r.GetString(r.GetOrdinal("Status")))
    };
}
