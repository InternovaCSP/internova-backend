using Microsoft.Data.SqlClient;
using Xunit;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Internova.Core.Entities;

namespace Internova.Tests;

public class DbVerificationTest
{
    private const string ConnectionString = "Data Source=localhost\\SQLEXPRESS;Initial Catalog=internova_db_local;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

    [Fact]
    public async Task CheckUsersInDb()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string sql = "SELECT user_id, email, role, is_approved FROM [User]";
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        System.Diagnostics.Debug.WriteLine("Users in DB:");
        while (await reader.ReadAsync())
        {
            var log = $"ID: {reader["user_id"]}, Email: {reader["email"]}, Role: {reader["role"]}, Approved: {reader["is_approved"]}";
            Console.WriteLine(log);
            Assert.NotNull(reader["email"]);
        }
    }
}
