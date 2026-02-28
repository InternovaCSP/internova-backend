using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Internova.Infrastructure.Data;

/// <summary>
/// Factory that provides Azure SQL Server connections for raw ADO.NET operations.
/// </summary>
public class DbConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Configure it via dotnet user-secrets.");

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
