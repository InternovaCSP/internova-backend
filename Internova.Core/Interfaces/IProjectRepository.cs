using Internova.Core.DTOs;
using Internova.Core.Entities;

namespace Internova.Core.Interfaces;

public interface IProjectRepository
{
    Task<Project> CreateProjectAsync(Project project);
    Task<bool> AddProjectMemberAsync(int projectId, int studentId, string role);
    Task<IEnumerable<ProjectResponseDto>> GetProjectsAsync(string? category);
    Task<Project?> GetProjectByIdAsync(int id);
    Task<bool> CreateJoinRequestAsync(int projectId, int studentId);
    Task<IEnumerable<ProjectRequestResponseDto>> GetStudentRequestsAsync(int studentId);
}
