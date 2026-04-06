using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

namespace Internova.Debug;

public class DbChecker
{
    public static async Task Main(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING") 
            ?? "Data Source=localhost\\SQLEXPRESS;Initial Catalog=internova_db_local;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        
        try 
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            Console.WriteLine("Connected to DB.");

            const string sql = "SELECT user_id, email, role, is_approved FROM [User]";
            await using var cmd = new SqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync();

            Console.WriteLine("Users in DB:");
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"ID: {reader["user_id"]}, Email: {reader["email"]}, Role: {reader["role"]}, Approved: {reader["is_approved"]}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
