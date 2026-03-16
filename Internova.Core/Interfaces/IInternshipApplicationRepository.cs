using Internova.Core.Entities;

namespace Internova.Core.Interfaces;

public interface IInternshipApplicationRepository
{
    Task<InternshipApplication> AddAsync(InternshipApplication application);
    Task<IEnumerable<InternshipApplication>> GetByStudentIdAsync(int studentId);
    Task<IDictionary<string, int>> GetPipelineStatsAsync(int studentId);
}
