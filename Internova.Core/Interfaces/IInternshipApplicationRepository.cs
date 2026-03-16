using Internova.Core.Entities;
using Internova.Core.Enums;

namespace Internova.Core.Interfaces;

public interface IInternshipApplicationRepository
{
    Task<InternshipApplication> AddAsync(InternshipApplication application);
    Task<IEnumerable<InternshipApplication>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<InternshipApplication>> GetByCompanyIdAsync(int companyId);
    Task<bool> UpdateStatusAsync(int applicationId, ApplicationStatus status);
    Task<IDictionary<string, int>> GetPipelineStatsAsync(int studentId);
    Task<IDictionary<string, string>> GetKpiStatsAsync(int studentId);
}
