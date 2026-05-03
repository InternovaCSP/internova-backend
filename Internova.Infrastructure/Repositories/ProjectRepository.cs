using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Internova.Infrastructure.Repositories;

public class ProjectRepository(DbConnectionFactory connectionFactory, ILogger<ProjectRepository> logger) : IProjectRepository
{
    public async Task<Project> CreateProjectAsync(Project project)
    {
        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO dbo.Project (leader_id, title, description, category, required_skills, team_size, status, is_approved)
            VALUES (@LeaderId, @Title, @Description, @Category, @RequiredSkills, @TeamSize, @Status, @IsApproved);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@LeaderId", project.LeaderId);
        cmd.Parameters.AddWithValue("@Title", project.Title);
        cmd.Parameters.AddWithValue("@Description", (object?)project.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Category", (object?)project.Category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RequiredSkills", (object?)project.RequiredSkills ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TeamSize", (object?)project.TeamSize ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", project.Status);
        cmd.Parameters.AddWithValue("@IsApproved", project.IsApproved);

        project.Id = (int)await cmd.ExecuteScalarAsync();
        return project;
    }

    public async Task<bool> AddProjectParticipationAsync(int projectId, int userId, string role, string status)
    {
        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM dbo.Project_Participation WHERE project_id = @ProjectId AND student_id = @UserId)
            BEGIN
                INSERT INTO dbo.Project_Participation (project_id, student_id, role, status, joined_at)
                VALUES (@ProjectId, @UserId, @Role, @Status, GETDATE());
            END";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ProjectId", projectId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Role", role);
        cmd.Parameters.AddWithValue("@Status", status);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetProjectsAsync(string? category)
    {
        var projects = new List<ProjectResponseDto>();
        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();

        string sql = @"
            SELECT p.project_id AS Id, p.leader_id AS LeaderId, p.title AS Title, 
                   p.description AS Description, p.category AS Category, 
                   p.required_skills AS RequiredSkills, p.team_size AS TeamSize,
                   p.status AS Status, p.is_approved AS IsApproved, 
                   u.full_name AS LeaderName
            FROM dbo.Project p
            LEFT JOIN dbo.[User] u ON p.leader_id = u.user_id";

        if (!string.IsNullOrWhiteSpace(category))
        {
            sql += " WHERE p.category = @Category";
        }

        sql += " ORDER BY p.project_id DESC"; // no created_at, using project_id to sort

        await using var cmd = new SqlCommand(sql, connection);
        if (!string.IsNullOrWhiteSpace(category))
        {
            cmd.Parameters.AddWithValue("@Category", category);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            projects.Add(new ProjectResponseDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                LeaderId = reader.GetInt32(reader.GetOrdinal("LeaderId")),
                LeaderName = reader.IsDBNull(reader.GetOrdinal("LeaderName")) ? "Unknown" : reader.GetString(reader.GetOrdinal("LeaderName")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "" : reader.GetString(reader.GetOrdinal("Category")),
                RequiredSkills = reader.IsDBNull(reader.GetOrdinal("RequiredSkills")) ? null : reader.GetString(reader.GetOrdinal("RequiredSkills")),
                TeamSize = reader.IsDBNull(reader.GetOrdinal("TeamSize")) ? null : reader.GetInt32(reader.GetOrdinal("TeamSize")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                IsApproved = reader.GetBoolean(reader.GetOrdinal("IsApproved"))
            });
        }

        return projects;
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT p.project_id AS Id, p.leader_id AS LeaderId, p.title AS Title, 
                   p.description AS Description, p.category AS Category, 
                   p.required_skills AS RequiredSkills, p.team_size AS TeamSize,
                   p.status AS Status, p.is_approved AS IsApproved, 
                   u.full_name AS LeaderName
            FROM dbo.Project p
            LEFT JOIN dbo.[User] u ON p.leader_id = u.user_id
            WHERE p.project_id = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new Project
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            LeaderId = reader.GetInt32(reader.GetOrdinal("LeaderId")),
            LeaderName = reader.IsDBNull(reader.GetOrdinal("LeaderName")) ? "Unknown" : reader.GetString(reader.GetOrdinal("LeaderName")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
            Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "" : reader.GetString(reader.GetOrdinal("Category")),
            RequiredSkills = reader.IsDBNull(reader.GetOrdinal("RequiredSkills")) ? null : reader.GetString(reader.GetOrdinal("RequiredSkills")),
            TeamSize = reader.IsDBNull(reader.GetOrdinal("TeamSize")) ? null : reader.GetInt32(reader.GetOrdinal("TeamSize")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            IsApproved = reader.GetBoolean(reader.GetOrdinal("IsApproved"))
        };
    }

    public async Task<IEnumerable<ProjectRequestResponseDto>> GetStudentParticipationsAsync(int userId)
    {
        var requests = new List<ProjectRequestResponseDto>();
        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT r.participation_id AS ParticipationId, r.project_id AS ProjectId, 
                   p.title AS ProjectTitle, p.category AS Category, 
                   r.role AS Role, r.status AS Status, r.joined_at AS JoinedAt
            FROM dbo.Project_Participation r
            JOIN dbo.Project p ON r.project_id = p.project_id
            WHERE r.student_id = @UserId
            ORDER BY r.joined_at DESC";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            requests.Add(new ProjectRequestResponseDto
            {
                ParticipationId = reader.GetInt32(reader.GetOrdinal("ParticipationId")),
                ProjectId = reader.GetInt32(reader.GetOrdinal("ProjectId")),
                ProjectTitle = reader.GetString(reader.GetOrdinal("ProjectTitle")),
                Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "" : reader.GetString(reader.GetOrdinal("Category")),
                Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? "" : reader.GetString(reader.GetOrdinal("Role")),
                Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "" : reader.GetString(reader.GetOrdinal("Status")),
                JoinedAt = reader.IsDBNull(reader.GetOrdinal("JoinedAt")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("JoinedAt"))
            });
        }

        return requests;
    }

    public async Task<bool> DeleteProjectAsync(int projectId)
    {
        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();

        // Delete participations first (FK constraint)
        const string deleteParticipationsSql = "DELETE FROM dbo.Project_Participation WHERE project_id = @ProjectId";
        await using var delPartCmd = new SqlCommand(deleteParticipationsSql, connection);
        delPartCmd.Parameters.AddWithValue("@ProjectId", projectId);
        await delPartCmd.ExecuteNonQueryAsync();

        // Delete the project
        const string deleteProjectSql = "DELETE FROM dbo.Project WHERE project_id = @ProjectId";
        await using var delCmd = new SqlCommand(deleteProjectSql, connection);
        delCmd.Parameters.AddWithValue("@ProjectId", projectId);
        var affected = await delCmd.ExecuteNonQueryAsync();
        return affected > 0;
    }
}
