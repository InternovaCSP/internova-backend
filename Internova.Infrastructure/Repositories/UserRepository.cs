using System.Data;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using MySql.Data.MySqlClient;

namespace Internova.Infrastructure.Repositories;

/// <summary>
/// ADO.NET implementation of IUserRepository.
/// </summary>
public class UserRepository(DbConnectionFactory connectionFactory) : IUserRepository
{
    private readonly DbConnectionFactory _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT UserId, FullName, Email, PasswordHash, Role, ContactNumber, CreatedAt FROM Users WHERE Email = @Email";
        
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@Email";
        parameter.Value = email;
        command.Parameters.Add(parameter);

        if (connection.State != ConnectionState.Open) await ((MySqlConnection)connection).OpenAsync();

        using var reader = await ((MySqlCommand)command).ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                UserId = reader.GetInt32("UserId"),
                FullName = reader.GetString("FullName"),
                Email = reader.GetString("Email"),
                PasswordHash = reader.GetString("PasswordHash"),
                Role = reader.GetString("Role"),
                ContactNumber = reader.IsDBNull(reader.GetOrdinal("ContactNumber")) ? null : reader.GetString("ContactNumber"),
                CreatedAt = reader.GetDateTime("CreatedAt")
            };
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Users (FullName, Email, PasswordHash, Role, ContactNumber, CreatedAt) 
            VALUES (@FullName, @Email, @PasswordHash, @Role, @ContactNumber, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        command.Parameters.Add(new MySqlParameter("@FullName", user.FullName));
        command.Parameters.Add(new MySqlParameter("@Email", user.Email));
        command.Parameters.Add(new MySqlParameter("@PasswordHash", user.PasswordHash));
        command.Parameters.Add(new MySqlParameter("@Role", user.Role));
        command.Parameters.Add(new MySqlParameter("@ContactNumber", (object?)user.ContactNumber ?? DBNull.Value));
        command.Parameters.Add(new MySqlParameter("@CreatedAt", user.CreatedAt));

        if (connection.State != ConnectionState.Open) await ((MySqlConnection)connection).OpenAsync();
        
        var result = await ((MySqlCommand)command).ExecuteScalarAsync();
        user.UserId = Convert.ToInt32(result);
        return user.UserId;
    }
}
