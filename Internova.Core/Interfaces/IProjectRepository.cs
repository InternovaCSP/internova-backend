using Internova.Core.DTOs;
using Internova.Core.Entities;

namespace Internova.Core.Interfaces;

public interface IProjectRepository
{
    Task<Project> CreateProjectAsync(Project project);
    Task<bool> AddProjectParticipationAsync(int projectId, int userId, string role, string status);
    Task<IEnumerable<ProjectResponseDto>> GetProjectsAsync(string? category);
    Task<Project?> GetProjectByIdAsync(int id);
    Task<IEnumerable<ProjectRequestResponseDto>> GetStudentParticipationsAsync(int userId);
    Task<bool> DeleteProjectAsync(int projectId);
}
