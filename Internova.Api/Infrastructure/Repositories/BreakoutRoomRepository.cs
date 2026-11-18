using Dapper;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;

namespace Internova.Api.Infrastructure.Repositories;

/// <summary>
/// Dapper repository for Breakout Rooms.
/// </summary>
public class BreakoutRoomRepository(DbConnectionFactory connectionFactory) : IBreakoutRoomRepository
{
    private const string SelectColumns = """
        SELECT r.room_id AS Id,
               r.organizer_id AS OrganizerId,
               r.title AS Title,
               r.description AS Description,
               r.scheduled_at AS ScheduledAt,
               r.meeting_link AS MeetingLink,
               r.status AS Status,
               r.award_skills AS AwardSkills,
               r.created_at AS CreatedAt,
               r.updated_at AS UpdatedAt,
               u.full_name AS OrganizerName
        FROM dbo.BreakoutRoom r
        JOIN dbo.[User] u ON r.organizer_id = u.user_id
        """;

    public async Task<BreakoutRoom> AddAsync(BreakoutRoom room)
    {
        const string sql = """
            INSERT INTO dbo.BreakoutRoom (
                organizer_id, title, description, scheduled_at, meeting_link, status, award_skills, created_at, updated_at)
            VALUES (
                @OrganizerId, @Title, @Description, @ScheduledAt, @MeetingLink, @Status, @AwardSkills, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = connectionFactory.CreateConnection();
        room.Id = await connection.ExecuteScalarAsync<int>(sql, room);
        return room;
    }

    public async Task<IEnumerable<BreakoutRoom>> GetActiveAsync()
    {
        const string sql = $"""
            {SelectColumns}
            WHERE r.status IN ('Scheduled', 'Active')
            ORDER BY r.scheduled_at ASC;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<BreakoutRoom>(sql);
    }

    public async Task<BreakoutRoom?> GetByIdAsync(int id)
    {
        const string sql = $"""
            {SelectColumns}
            WHERE r.room_id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<BreakoutRoom>(sql, new { Id = id });
    }

    public async Task<bool> UpdateAsync(BreakoutRoom room)
    {
        const string sql = """
            UPDATE dbo.BreakoutRoom
            SET title = @Title,
                description = @Description,
                scheduled_at = @ScheduledAt,
                meeting_link = @MeetingLink,
                status = @Status,
                award_skills = @AwardSkills,
                updated_at = @UpdatedAt
            WHERE room_id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(sql, room);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM dbo.BreakoutRoom WHERE room_id = @Id;";

        await using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}
