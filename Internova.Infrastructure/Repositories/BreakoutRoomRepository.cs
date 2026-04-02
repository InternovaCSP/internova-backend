using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Internova.Infrastructure.Repositories;

/// <summary>
/// Dapper-based repository for Breakout Rooms.
/// </summary>
public class BreakoutRoomRepository : IBreakoutRoomRepository
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<BreakoutRoomRepository> _logger;

    public BreakoutRoomRepository(DbConnectionFactory connectionFactory, ILogger<BreakoutRoomRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<BreakoutRoom> AddAsync(BreakoutRoom room)
    {
        const string sql = @"
            INSERT INTO BreakoutRoom (organizer_id, title, description, scheduled_at, meeting_link, status, award_skills, created_at, updated_at)
            VALUES (@OrganizerId, @Title, @Description, @ScheduledAt, @MeetingLink, @Status, @AwardSkills, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        using var connection = _connectionFactory.CreateConnection();
        room.Id = await connection.ExecuteScalarAsync<int>(sql, room);
        return room;
    }

    public async Task<IEnumerable<BreakoutRoom>> GetActiveAsync()
    {
        const string sql = @"
            SELECT r.room_id as Id, r.organizer_id as OrganizerId, r.title, r.description, 
                   r.scheduled_at as ScheduledAt, r.meeting_link as MeetingLink, 
                   r.status, r.award_skills as AwardSkills, r.created_at as CreatedAt, 
                   r.updated_at as UpdatedAt, u.full_name as OrganizerName
            FROM BreakoutRoom r
            JOIN [User] u ON r.organizer_id = u.user_id
            WHERE r.status IN ('Scheduled', 'Active')
            ORDER BY r.scheduled_at ASC";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<BreakoutRoom>(sql);
    }

    public async Task<BreakoutRoom?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT r.room_id as Id, r.organizer_id as OrganizerId, r.title, r.description, 
                   r.scheduled_at as ScheduledAt, r.meeting_link as MeetingLink, 
                   r.status, r.award_skills as AwardSkills, r.created_at as CreatedAt, 
                   r.updated_at as UpdatedAt, u.full_name as OrganizerName
            FROM BreakoutRoom r
            JOIN [User] u ON r.organizer_id = u.user_id
            WHERE r.room_id = @Id";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<BreakoutRoom>(sql, new { Id = id });
    }

    public async Task<bool> UpdateAsync(BreakoutRoom room)
    {
        const string sql = @"
            UPDATE BreakoutRoom
            SET title = @Title, description = @Description, scheduled_at = @ScheduledAt, 
                meeting_link = @MeetingLink, status = @Status, award_skills = @AwardSkills, 
                updated_at = @UpdatedAt
            WHERE room_id = @Id";

        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(sql, room);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM BreakoutRoom WHERE room_id = @Id";
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}
