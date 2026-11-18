using Dapper;
using Internova.Api.Application.DTOs;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;

namespace Internova.Api.Infrastructure.Repositories;

public class ProjectRepository(DbConnectionFactory connectionFactory) : IProjectRepository
{
    public async Task<Project> CreateProjectAsync(Project project)
    {
        const string sql = """
            INSERT INTO dbo.Project (leader_id, title, description, category, required_skills, team_size, status, is_approved)
            VALUES (@LeaderId, @Title, @Description, @Category, @RequiredSkills, @TeamSize, @Status, @IsApproved);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = connectionFactory.CreateConnection();
        project.Id = await connection.ExecuteScalarAsync<int>(sql, project);
        return project;
    }

    public async Task<bool> AddProjectParticipationAsync(int projectId, int studentId, string role, string status)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Project_Participation WHERE project_id = @ProjectId AND student_id = @StudentId)
            BEGIN
                INSERT INTO dbo.Project_Participation (project_id, student_id, role, status, joined_at)
                VALUES (@ProjectId, @StudentId, @Role, @Status, GETUTCDATE());
            END
            """;

        await using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, new
        {
            ProjectId = projectId,
            StudentId = studentId,
            Role = role,
            Status = status
        });
        return affected > 0;
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetProjectsAsync(string? category)
    {
        const string sql = """
            SELECT p.project_id AS Id,
                   p.leader_id AS LeaderId,
                   ISNULL(u.full_name, 'Unknown') AS LeaderName,
                   p.title AS Title,
                   ISNULL(p.description, '') AS Description,
                   ISNULL(p.category, '') AS Category,
                   p.required_skills AS RequiredSkills,
                   p.team_size AS TeamSize,
                   p.status AS Status,
                   p.is_approved AS IsApproved
            FROM dbo.Project p
            LEFT JOIN dbo.[User] u ON p.leader_id = u.user_id
            WHERE (@Category IS NULL OR p.category = @Category)
            ORDER BY p.project_id DESC;
            """;

        var filter = string.IsNullOrWhiteSpace(category) ? null : category;
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<ProjectResponseDto>(sql, new { Category = filter });
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        const string sql = """
            SELECT p.project_id AS Id,
                   p.leader_id AS LeaderId,
                   ISNULL(u.full_name, 'Unknown') AS LeaderName,
                   p.title AS Title,
                   ISNULL(p.description, '') AS Description,
                   ISNULL(p.category, '') AS Category,
                   p.required_skills AS RequiredSkills,
                   p.team_size AS TeamSize,
                   p.status AS Status,
                   p.is_approved AS IsApproved
            FROM dbo.Project p
            LEFT JOIN dbo.[User] u ON p.leader_id = u.user_id
            WHERE p.project_id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Project>(sql, new { Id = id });
    }

    public async Task<IEnumerable<ProjectRequestResponseDto>> GetStudentParticipationsAsync(int studentId)
    {
        const string sql = """
            SELECT r.participation_id AS ParticipationId,
                   r.project_id AS ProjectId,
                   p.title AS ProjectTitle,
                   ISNULL(p.category, '') AS Category,
                   ISNULL(r.role, '') AS Role,
                   ISNULL(r.status, '') AS Status,
                   ISNULL(r.joined_at, GETUTCDATE()) AS JoinedAt
            FROM dbo.Project_Participation r
            JOIN dbo.Project p ON r.project_id = p.project_id
            WHERE r.student_id = @StudentId
            ORDER BY r.joined_at DESC;
            """;

        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<ProjectRequestResponseDto>(sql, new { StudentId = studentId });
    }

    public async Task<bool> DeleteProjectAsync(int projectId)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            await connection.ExecuteAsync(
                "DELETE FROM dbo.Project_Participation WHERE project_id = @ProjectId;",
                new { ProjectId = projectId },
                transaction);

            var affected = await connection.ExecuteAsync(
                "DELETE FROM dbo.Project WHERE project_id = @ProjectId;",
                new { ProjectId = projectId },
                transaction);

            await transaction.CommitAsync();
            return affected > 0;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
