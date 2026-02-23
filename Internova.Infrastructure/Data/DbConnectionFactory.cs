using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

namespace Internova.Infrastructure.Data;

/// <summary>
/// Factory to provide MySql database connections for ADO.NET.
/// </summary>
public class DbConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("Default") 
        ?? throw new InvalidOperationException("Connection string 'Default' not found.");

    public IDbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }
}
