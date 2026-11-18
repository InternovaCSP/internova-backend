using Dapper;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;

namespace Internova.Api.Infrastructure.Repositories;

public class SeminarRepository(DbConnectionFactory connectionFactory) : ISeminarRepository
{
    private const string SelectColumns = """
        SELECT sr.id AS Id,
               sr.student_id AS StudentId,
               u.full_name AS StudentName,
               sr.topic AS Topic,
               sr.description AS Description,
               sr.status AS Status,
               sr.threshold AS Threshold,
               sr.created_at AS CreatedAt,
               sr.updated_at AS UpdatedAt,
               (SELECT COUNT(*) FROM dbo.Seminar_Vote WHERE request_id = sr.id) AS VoteCount
        FROM dbo.Seminar_Request sr
        JOIN dbo.[User] u ON sr.student_id = u.user_id
        """;

    public async Task<IEnumerable<SeminarRequest>> GetAllAsync()
    {
        const string sql = $"""
            {SelectColumns}
            ORDER BY sr.created_at DESC;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<SeminarRequest>(sql);
    }

    public async Task<SeminarRequest?> GetByIdAsync(int id)
    {
        const string sql = $"""
            {SelectColumns}
            WHERE sr.id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<SeminarRequest>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(SeminarRequest request)
    {
        const string sql = """
            INSERT INTO dbo.Seminar_Request (student_id, topic, description, status, threshold, created_at, updated_at)
            VALUES (@StudentId, @Topic, @Description, @Status, @Threshold, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, request);
    }

    public async Task<bool> VoteAsync(int requestId, int studentId)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Seminar_Vote WHERE request_id = @RequestId AND student_id = @StudentId)
            BEGIN
                INSERT INTO dbo.Seminar_Vote (request_id, student_id, voted_at)
                VALUES (@RequestId, @StudentId, @VotedAt);
            END
            """;

        await using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, new
        {
            RequestId = requestId,
            StudentId = studentId,
            VotedAt = DateTime.UtcNow
        });
        return affected > 0;
    }

    public async Task<int> GetVoteCountAsync(int requestId)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.Seminar_Vote WHERE request_id = @RequestId;";

        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { RequestId = requestId });
    }

    public async Task<bool> HasStudentVotedAsync(int requestId, int studentId)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.Seminar_Vote WHERE request_id = @RequestId AND student_id = @StudentId;";

        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { RequestId = requestId, StudentId = studentId }) > 0;
    }

    public async Task<IReadOnlySet<int>> GetVotedRequestIdsAsync(int studentId)
    {
        const string sql = "SELECT request_id FROM dbo.Seminar_Vote WHERE student_id = @StudentId;";

        await using var connection = connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<int>(sql, new { StudentId = studentId });
        return ids.ToHashSet();
    }

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        const string sql = """
            UPDATE dbo.Seminar_Request
            SET status = @Status, updated_at = @UpdatedAt
            WHERE id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(sql, new
        {
            Id = id,
            Status = status,
            UpdatedAt = DateTime.UtcNow
        });
        return rows > 0;
    }
}
