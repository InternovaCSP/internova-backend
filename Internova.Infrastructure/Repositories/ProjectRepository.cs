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
            INSERT INTO dbo.Project (creator_id, title, description, category, status, created_at)
            VALUES (@CreatorId, @Title, @Description, @Category, @Status, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CreatorId", project.CreatorId);
        cmd.Parameters.AddWithValue("@Title", project.Title);
        cmd.Parameters.AddWithValue("@Description", (object?)project.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Category", (object?)project.Category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", project.Status);
        cmd.Parameters.AddWithValue("@CreatedAt", project.CreatedAt);

        project.Id = (int)await cmd.ExecuteScalarAsync();
        return project;
    }

    public async Task<bool> AddProjectMemberAsync(int projectId, int studentId, string role)
    {
        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO dbo.Project_Member (project_id, student_id, role)
            VALUES (@ProjectId, @StudentId, @Role);";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ProjectId", projectId);
        cmd.Parameters.AddWithValue("@StudentId", studentId);
        cmd.Parameters.AddWithValue("@Role", role);

        try
        {
            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // PK violation meaning already member
        {
            logger.LogWarning("Member already exists for ProjectId: {ProjectId}, StudentId: {StudentId}", projectId, studentId);
            return false;
        }
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetProjectsAsync(string? category)
    {
        var projects = new List<ProjectResponseDto>();
        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();

        string sql = @"
            SELECT p.project_id AS Id, p.creator_id AS CreatorId, p.title AS Title, 
                   p.description AS Description, p.category AS Category, p.status AS Status, 
                   p.created_at AS CreatedAt, u.full_name AS CreatorName
            FROM dbo.Project p
            LEFT JOIN dbo.[User] u ON p.creator_id = u.user_id";

        if (!string.IsNullOrWhiteSpace(category))
        {
            sql += " WHERE p.category = @Category";
        }

        sql += " ORDER BY p.created_at DESC";

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
                CreatorId = reader.GetInt32(reader.GetOrdinal("CreatorId")),
                CreatorName = reader.IsDBNull(reader.GetOrdinal("CreatorName")) ? "Unknown" : reader.GetString(reader.GetOrdinal("CreatorName")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "" : reader.GetString(reader.GetOrdinal("Category")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
        }

        return projects;
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT p.project_id AS Id, p.creator_id AS CreatorId, p.title AS Title, 
                   p.description AS Description, p.category AS Category, p.status AS Status, 
                   p.created_at AS CreatedAt, u.full_name AS CreatorName
            FROM dbo.Project p
            LEFT JOIN dbo.[User] u ON p.creator_id = u.user_id
            WHERE p.project_id = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new Project
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            CreatorId = reader.GetInt32(reader.GetOrdinal("CreatorId")),
            CreatorName = reader.IsDBNull(reader.GetOrdinal("CreatorName")) ? "Unknown" : reader.GetString(reader.GetOrdinal("CreatorName")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
            Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "" : reader.GetString(reader.GetOrdinal("Category")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    public async Task<bool> CreateJoinRequestAsync(int projectId, int studentId)
    {
        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM dbo.Project_Request WHERE project_id = @ProjectId AND student_id = @StudentId)
            BEGIN
                INSERT INTO dbo.Project_Request (project_id, student_id, status, requested_at)
                VALUES (@ProjectId, @StudentId, 'Pending', GETDATE());
            END";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ProjectId", projectId);
        cmd.Parameters.AddWithValue("@StudentId", studentId);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<IEnumerable<ProjectRequestResponseDto>> GetStudentRequestsAsync(int studentId)
    {
        var requests = new List<ProjectRequestResponseDto>();
        await using var connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT r.request_id AS RequestId, r.project_id AS ProjectId, 
                   p.title AS ProjectTitle, p.category AS Category, 
                   r.status AS Status, r.requested_at AS RequestedAt
            FROM dbo.Project_Request r
            JOIN dbo.Project p ON r.project_id = p.project_id
            WHERE r.student_id = @StudentId
            ORDER BY r.requested_at DESC";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@StudentId", studentId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            requests.Add(new ProjectRequestResponseDto
            {
                RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
                ProjectId = reader.GetInt32(reader.GetOrdinal("ProjectId")),
                ProjectTitle = reader.GetString(reader.GetOrdinal("ProjectTitle")),
                Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "" : reader.GetString(reader.GetOrdinal("Category")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                RequestedAt = reader.IsDBNull(reader.GetOrdinal("RequestedAt")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("RequestedAt"))
            });
        }

        return requests;
    }
}
