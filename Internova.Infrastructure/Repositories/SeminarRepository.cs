using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Internova.Infrastructure.Repositories;

public class SeminarRepository(DbConnectionFactory connectionFactory) : ISeminarRepository
{
    private readonly DbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<IEnumerable<SeminarRequest>> GetAllAsync()
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT sr.*, u.full_name as StudentName, 
                   (SELECT COUNT(*) FROM Seminar_Vote WHERE request_id = sr.id) as VoteCount
            FROM Seminar_Request sr
            JOIN [User] u ON sr.student_id = u.user_id
            ORDER BY sr.created_at DESC";

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        
        var requests = new List<SeminarRequest>();
        while (await reader.ReadAsync())
        {
            requests.Add(MapSeminarRequest(reader));
        }
        return requests;
    }

    public async Task<SeminarRequest?> GetByIdAsync(int id)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT sr.*, u.full_name as StudentName,
                   (SELECT COUNT(*) FROM Seminar_Vote WHERE request_id = sr.id) as VoteCount
            FROM Seminar_Request sr
            JOIN [User] u ON sr.student_id = u.user_id
            WHERE sr.id = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapSeminarRequest(reader);
    }

    public async Task<int> CreateAsync(SeminarRequest request)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO Seminar_Request (student_id, topic, description, status, threshold, created_at, updated_at)
            VALUES (@StudentId, @Topic, @Description, @Status, @Threshold, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@StudentId", request.StudentId);
        cmd.Parameters.AddWithValue("@Topic", request.Topic);
        cmd.Parameters.AddWithValue("@Description", request.Description);
        cmd.Parameters.AddWithValue("@Status", request.Status);
        cmd.Parameters.AddWithValue("@Threshold", request.Threshold);
        cmd.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);
        cmd.Parameters.AddWithValue("@UpdatedAt", request.UpdatedAt);

        return (int)await cmd.ExecuteScalarAsync();
    }

    public async Task<bool> VoteAsync(int requestId, int studentId)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        // Check if already voted (redundancy check, although constrained in DB)
        if (await HasStudentVotedAsync(requestId, studentId)) return false;

        const string sql = @"
            INSERT INTO Seminar_Vote (request_id, student_id, voted_at)
            VALUES (@RequestId, @StudentId, @VotedAt)";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@RequestId", requestId);
        cmd.Parameters.AddWithValue("@StudentId", studentId);
        cmd.Parameters.AddWithValue("@VotedAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<int> GetVoteCountAsync(int requestId)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT COUNT(*) FROM Seminar_Vote WHERE request_id = @RequestId";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@RequestId", requestId);

        return (int)await cmd.ExecuteScalarAsync();
    }

    public async Task<bool> HasStudentVotedAsync(int requestId, int studentId)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT COUNT(*) FROM Seminar_Vote WHERE request_id = @RequestId AND student_id = @StudentId";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@RequestId", requestId);
        cmd.Parameters.AddWithValue("@StudentId", studentId);

        return (int)await cmd.ExecuteScalarAsync() > 0;
    }

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "UPDATE Seminar_Request SET status = @status, updated_at = @updatedAt WHERE id = @Id";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Status", status);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@Id", id);

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    private static SeminarRequest MapSeminarRequest(SqlDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("id")),
        StudentId = r.GetInt32(r.GetOrdinal("student_id")),
        StudentName = r.GetString(r.GetOrdinal("StudentName")),
        Topic = r.GetString(r.GetOrdinal("topic")),
        Description = r.GetString(r.GetOrdinal("description")),
        Status = r.GetString(r.GetOrdinal("status")),
        Threshold = r.GetInt32(r.GetOrdinal("threshold")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("created_at")),
        UpdatedAt = r.GetDateTime(r.GetOrdinal("updated_at")),
        VoteCount = r.GetInt32(r.GetOrdinal("VoteCount"))
    };
}
