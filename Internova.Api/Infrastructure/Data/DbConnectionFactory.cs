using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Internova.Api.Infrastructure.Data;

/// <summary>
/// Factory that provides SQL Server connections for Dapper data-access operations.
/// </summary>
public class DbConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' not found. Configure it in appsettings.Development.json or User Secrets.");

    public SqlConnection CreateConnection() => new(_connectionString);
}
