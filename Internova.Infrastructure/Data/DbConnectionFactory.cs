using System.Data;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace Internova.Infrastructure.Data;

/// <summary>
/// Factory that provides local MySQL connections for raw ADO.NET operations.
/// </summary>
public class DbConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Add it to appsettings.Development.json.");

    public IDbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }
}
